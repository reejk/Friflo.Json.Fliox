﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Auth;
using Friflo.Json.Fliox.DB.Auth.Rights;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.UserAuth
{
    internal class AuthCred {
        internal readonly   string          token;
        
        internal AuthCred (string token) {
            this.token  = token;
        }
    }
    
    internal class UserCredentials {
        internal readonly   string              token;
        internal readonly   Authorizer          authorizer;
        internal readonly   HashSet<JsonKey>    clients = new HashSet<JsonKey>(JsonKey.Equality);
        
        internal UserCredentials (string token, Authorizer authorizer) {
            this.token      = token;
            this.authorizer = authorizer;
        }
    }
    
    public interface IUserAuth {
        Task<AuthenticateUserResult> AuthenticateUser(AuthenticateUser value);
    }
    
    /// <summary>
    /// Performs user authentication by validating the "userId" and the "token" assigned to an <see cref="Client.EntityStore"/>
    /// <br></br>
    /// If authentication succeed it set the <see cref="AuthState.Authorizer"/> derived from the roles assigned to the user.
    /// If authentication fails the given default <see cref="Authorizer"/> is used for the user.
    /// </summary>
    public class UserAuthenticator : Authenticator
    {
        private   readonly  UserStore                                       userStore;
        private   readonly  IUserAuth                                       userAuth;
        internal  readonly  ConcurrentDictionary<JsonKey, UserCredentials>  credByUser;
        private   readonly  Authorizer                                      unknown;
        private   readonly  ConcurrentDictionary<string,  Authorizer>       authorizerByRole;

        public UserAuthenticator (UserStore userStore, IUserAuth userAuth, Authorizer unknown = null) {
            this.userStore      = userStore;
            this.userAuth       = userAuth;
            credByUser          = new ConcurrentDictionary <JsonKey, UserCredentials>(JsonKey.Equality);
            this.unknown        = unknown ?? new AuthorizeDeny();
            authorizerByRole    = new ConcurrentDictionary <string, Authorizer>();
        }
        
        public async Task ValidateRoles() {
            var queryRoles = userStore.roles.QueryAll();
            await userStore.TrySync().ConfigureAwait(false);
            Dictionary<string, Role> roles = queryRoles.Results;
            foreach (var pair in roles) {
                var role = pair.Value;
                foreach (var right in role.rights) {
                    if (!(right is RightPredicate rightPredicates))
                        break;
                    foreach (var predicateName in rightPredicates.names) {
                        if (!registeredPredicates.ContainsKey(predicateName)) {
                            throw new InvalidOperationException($"unknown authorization predicate: {predicateName}");
                        }
                    }
                }
            }
        }

        private const string InvalidUserToken = "Authentication failed";
        
        public override async Task Authenticate(SyncRequest syncRequest, MessageContext messageContext)
        {
            var userId = syncRequest.userId;
            if (userId.IsNull()) {
                messageContext.authState.SetFailed("user authentication requires 'user' id", unknown);
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                messageContext.authState.SetFailed("user authentication requires 'token'", unknown);
                return;
            }
            if (credByUser.TryGetValue(userId, out UserCredentials credential)) {
                if (credential.token != token) {
                    messageContext.authState.SetFailed(InvalidUserToken, unknown);
                    return;
                }
                messageContext.authState.SetSuccess(credential.authorizer);
                return;
            }
            var command = new AuthenticateUser { userId = userId, token = token };
            var result  = await userAuth.AuthenticateUser(command).ConfigureAwait(false);
            
            if (result.isValid) {
                var authCred    = new AuthCred(token);
                var authorizer  = await GetAuthorizer(userId).ConfigureAwait(false);
                credential      = new UserCredentials (authCred.token, authorizer);
                credByUser.TryAdd(userId,      credential);
            }
            
            if (credential == null || token != credential.token) {
                messageContext.authState.SetFailed(InvalidUserToken, unknown);
                return;
            }
            messageContext.authState.SetSuccess(credential.authorizer);
        }
        
        public override bool ValidateClientId(MessageContext messageContext) {
            if (messageContext.userId.IsNull())
                return false;
            if (!credByUser.TryGetValue(messageContext.userId, out UserCredentials userCredentials))
                return false;
            if (messageContext.clientId.IsNull()){
                return true;
            }
            return userCredentials.clients.Contains(messageContext.clientId);
        }
        
        public override bool EnsureValidClientId(ClientController clientController, MessageContext messageContext, out string error) {
            if (!messageContext.clientIdValid) {
                error = $"invalid client id. 'clt': {messageContext.clientId}";
                return false;
            }
            error = null;
            if (!credByUser.TryGetValue(messageContext.userId, out UserCredentials userCredentials)) {
                throw new InvalidOperationException("unexpected. userId already validated");
            }
            if (!messageContext.clientId.IsNull()) {
                return true; // clientId already validated -> can be used in further processing
            }
            var clientId = clientController.NewClientId();
            userCredentials.clients.Add(clientId);
            messageContext.clientId = clientId;
            return true;
        }

        private async Task<Authorizer> GetAuthorizer(JsonKey userId) {
            var readPermission = userStore.permissions.Read().Find(userId);
            await userStore.Sync().ConfigureAwait(false);
            UserPermission permission = readPermission.Result;
            var roles = permission.roles;
            if (roles == null || roles.Count == 0) {
                return unknown;
            }
            await AddNewRoles(roles).ConfigureAwait(false);
            var authorizers = new List<Authorizer>(roles.Count);
            foreach (var role in roles) {
                // existence is checked already in AddNewRoles()
                authorizerByRole.TryGetValue(role, out Authorizer authorizer);
                authorizers.Add(authorizer);
            }
            if (authorizers.Count == 1)
                return authorizers[0];
            var any = new AuthorizeAny(authorizers);
            return any;
        }
        
        private async Task AddNewRoles(List<string> roles) {
            var newRoles = new List<string>();
            foreach (var role in roles) {
                if (!authorizerByRole.TryGetValue(role, out _)) {
                    newRoles.Add(role);
                }
            }
            if (newRoles.Count == 0)
                return;
            var readRoles = userStore.roles.Read().FindRange(newRoles);
            await userStore.Sync().ConfigureAwait(false);
            foreach (var newRolePair in readRoles.Results) {
                string role     = newRolePair.Key;
                Role newRole    = newRolePair.Value;
                if (newRole == null)
                    throw new InvalidOperationException($"authorization role not found: '{role}'");
                var authorizers = new List<Authorizer>(newRole.rights.Count);
                foreach (var right in newRole.rights) {
                    Authorizer authorizer;
                    if (right is RightPredicate predicates) {
                        authorizer = GetPredicatesAuthorizer(predicates);
                    } else {
                        authorizer = right.ToAuthorizer();
                    }
                    authorizers.Add(authorizer);
                }
                if (authorizers.Count == 1) {
                    authorizerByRole.TryAdd(role, authorizers[0]);
                } else {
                    var any = new AuthorizeAny(authorizers);
                    authorizerByRole.TryAdd(role, any);
                }
            }
        }
        
        private Authorizer GetPredicatesAuthorizer(RightPredicate right) {
            var authorizers = new List<Authorizer>(right.names.Count);
            foreach (var predicateName in right.names) {
                if (!registeredPredicates.TryGetValue(predicateName, out var predicate)) {
                    throw new InvalidOperationException($"unknown authorization predicate: {predicateName}");
                }
                authorizers.Add(predicate);
            }
            if (authorizers.Count == 1) {
                return authorizers[0];
            }
            return new AuthorizeAny(authorizers);
        }
    }
}