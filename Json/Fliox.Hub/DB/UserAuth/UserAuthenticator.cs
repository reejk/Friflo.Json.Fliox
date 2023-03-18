﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Host.Auth.Rights;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Utils;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    public interface IUserAuth {
        Task<AuthResult> AuthenticateAsync(Credentials value);
    }

    /// <summary>
    /// Performs user authentication by validating the <b>user</b> and <b>token</b> of every request<br/>
    /// Performs authorization for successful authenticated users by applying their assigned permissions.<br/>
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>
    ///     To avoid loosing access to the Hub accidentally following operations are not permitted:<br/>
    ///     - Delete user <b>admin</b><br/>
    ///     - Change permission <b>admin</b><br/>
    ///     - Change role <b>hub-admin</b>
    ///   </item>
    ///   <item>
    ///     The permission <b>all-users</b> - if available - is used for authenticated and anonymous users.<br/>
    ///   </item>
    ///   <item>
    ///     User permissions and roles are transparently defined and stored in the <b>user_db</b>.<br/>
    ///     So authorization is fully documented by these two containers.
    ///   </item>
    ///   <item>
    ///     User permissions and roles are cached for successful authenticated users.<br/>
    ///     This enables instant task authorization and reduces the number of reads to the <b>user_db</b> significant.
    ///   </item>
    /// </list>
    /// </remarks>
    public sealed class UserAuthenticator : Authenticator, IDisposable
    {
        // --- private / internal
        internal  readonly  FlioxHub                                    userHub;
        private   readonly  IUserAuth                                   userAuth;
        internal  readonly  ConcurrentDictionary<string, UserAuthRole>  roleCache;
        /// <summary><see cref="User"/> instance used for all unauthenticated users</summary>
        private   readonly  User                                        anonymous;
        /// <summary>Contains authorization permissions for all users</summary>
        private   readonly  User                                        allUsers;
        private   readonly  User                                        authenticatedUsers;

        public static class ID {
            public  const string    AllUsers            = "all-users";
            public  const string    AuthenticatedUsers  = "authenticate-users";
            public  const string    Admin               = "admin";
            public  const string    HubAdmin            = "hub-admin";
            public  const string    ClusterInfo         = "cluster-info";
        }

        public UserAuthenticator (EntityDatabase userDatabase, SharedEnv env = null, IUserAuth userAuth = null)
        {
            var service = userDatabase.service;
            if (!(service is UserDBService))
                throw new ArgumentException($"userDatabase requires service: {nameof(UserDBService)}. was: {service.GetType().Name}");
            var sharedEnv           = env  ?? SharedEnv.Default;
            userDatabase.Schema     = new DatabaseSchema(typeof(UserStore));
            userHub        	        = new FlioxHub(userDatabase, sharedEnv);
            userHub.Authenticator   = new UserDatabaseAuthenticator(userDatabase.name);  // authorize access to userDatabase
            this.userAuth           = userAuth;
            roleCache               = new ConcurrentDictionary <string, UserAuthRole>();
            anonymous               = new User(User.AnonymousId);
            allUsers                = new User(ID.AllUsers);
            authenticatedUsers      = new User(ID.AuthenticatedUsers);
        }

        /// <summary>
        /// Set default admin permissions records in the user database to enable Hub access with user admin.<br/>
        /// </summary>
        /// <remarks>
        /// - create user credential: <b>admin</b> if not exist<br/>
        /// - set user permission: <b>admin</b> with role <b>hub-admin</b><br/>
        /// - set role: <b>hub-admin</b> granting full access to all databases<br/>
        /// <br/>
        /// This enables access to all Hub databases as user <b>admin</b> without accessing the user database directly.
        /// </remarks> 
        public UserAuthenticator SetAdminPermissions(string token = "admin") {
            var userStore           = new UserStore(userHub) { UserId = UserStore.Server };
            userStore.WritePretty   = true;
            var adminCredential     = new UserCredential {
                id      = ID.Admin,
                token   = token,
            };
            userStore.credentials.Create(adminCredential);
            
            // --- admin / hub-admin
            var adminPermission     = new UserPermission {
                id      = ID.Admin,
                roles   = new List<string> { ID.HubAdmin }
            };
            var hubAdmin            = new Role {
                id          = ID.HubAdmin,
                taskRights  = new List<TaskRight> { new DbFullRight { database = "*"} },
                hubRights   = new HubRights { queueEvents = true },
                description = "Grant unrestricted access to all databases"
            };
            var upsertPermission    = userStore.permissions.Upsert(adminPermission);
            var upsertRole          = userStore.roles.Upsert(hubAdmin);
            var sync = userStore.TrySyncTasks().Result;
            if (upsertPermission.Success && upsertRole.Success) {
                return this;                
            }
            throw new InvalidOperationException($"Failed writing default permissions. error: {sync.Message}");
        }
        
        public UserAuthenticator SetClusterPermissions(string clusterDB) {
            var userStore           = new UserStore(userHub) { UserId = UserStore.Server };
            userStore.WritePretty   = true;

            // --- admin / hub-admin
            var authenticatedPermission     = new UserPermission {
                id      = ID.AuthenticatedUsers,
                roles   = new List<string> { ID.ClusterInfo }
            };
            var clusterInfo            = new Role {
                id          = ID.ClusterInfo,
                taskRights  = new List<TaskRight> { new DbFullRight { database = clusterDB} },
                description = "Allow reading the cluster database"
            };
            userStore.permissions.Create(authenticatedPermission);
            userStore.roles.Create(clusterInfo);

            userStore.TrySyncTasks().Wait();
            return this;
        }

        public void Dispose() {
            userHub.Dispose();
        }
        
        /// <summary>
        /// Subscribe changes to <b>user_db</b> <see cref="UserStore.permissions"/> and <see cref="UserStore.roles"/> to 
        /// applying these changes to users instantaneously.<br/>
        /// <br/>
        /// Without subscribing <b>user_db</b> changes they are effective after a call to <see cref="UserStore.ClearAuthCache"/>
        /// or after a server restart.
        /// </summary>
        public UserAuthenticator SubscribeUserDbChanges(EventDispatcher eventDispatcher) {
            userHub.EventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            var subscriber          = new UserStoreSubscriber(this);
            subscriber.SetupSubscriptions ().Wait();
            return this;
        }

        public async Task<List<string>> ValidateUserDb(HashSet<string> databases) {
            var predicates  = registeredPredicates.Keys.ToHashSet();
            var errors      = new List<string>();
            using(var userStore = new UserStore (userHub)) {
                userStore.UserId = UserStore.AuthenticationUser;
                userStore.permissions.QueryAll();
                userStore.roles.QueryAll();
                var result = await userStore.TrySyncTasks().ConfigureAwait(false);
                
                if (!result.Success) {
                    foreach (var task in result.failed) {
                        errors.Add(task.Error.Message);
                    }
                    return errors;
                }
                var roleValidation = new RoleValidation(databases, predicates, errors);
                ValidatePermissions (userStore, errors);
                ValidateRoles       (userStore, roleValidation);
                return  errors;
            }
        }
        
        private static void ValidatePermissions(UserStore userStore, List<string> errors) {
            var permissions = userStore.permissions.Local.Entities;
            foreach (var permission in permissions) {
                foreach (var role in permission.roles) {
                    if (userStore.roles.Local.ContainsKey(role))
                        continue;
                    var error = $"role not found. role: '{role}' in permission: '{permission.id}'";
                    errors.Add(error);
                }
            }
        }
        
        private static void ValidateRoles(UserStore userStore, RoleValidation roleValidation) {
            var roles = userStore.roles.Local.Entities;
            foreach (var role in roles) {
                var validation = new RoleValidation(roleValidation, role);
                foreach (var taskRight in role.taskRights) {
                    taskRight.Validate(validation);
                }
            }
        }
        
        internal void InvalidateUsers(IEnumerable<string> userIds) {
            var ids = userIds.Select(id => new ShortString(id)).ToHashSet(ShortString.Equality);
            InvalidateUsers(ids);
        }
        
        internal void InvalidateUsers(ICollection<ShortString> userIds) {
            bool invalidateAll = false;
            if (userIds.Contains(allUsers.userId)) {
                allUsers.invalidated = true;
                invalidateAll = true;
            }
            if (userIds.Contains(authenticatedUsers.userId)) {
                authenticatedUsers.invalidated = true;
                invalidateAll = true;
            }
            if (invalidateAll) {
                foreach (var user in users) { user.Value.invalidated = true; }
                return;
            }
            foreach (var userId in userIds) {
                if (!users.TryGetValue(userId, out var user))
                    continue;
                user.invalidated = true;
            }
        }

        // --- Authenticator 
        /// <summary>
        /// <see cref="Authenticate"/> can run synchronous if already successful authenticated or a general
        /// authentication error occurred.
        /// </summary>
        public override bool IsSynchronous(SyncRequest syncRequest) {
            return PreAuth(syncRequest, out syncRequest.intern.preAuthType, out syncRequest.intern.preAuthUser);
        }
        
        public override void Authenticate (SyncRequest syncRequest, SyncContext syncContext) {
            bool isSync = AuthenticateSynchronous(syncRequest.intern.preAuthType, syncRequest.intern.preAuthUser, syncContext);
            if (isSync)
                return;
            throw new InvalidOperationException("authentication cannot be executed synchronously");
        }
        
        /// <summary>returns true if authentication can be executed synchronously</summary>
        private bool PreAuth(SyncRequest syncRequest, out PreAuthType type, out User user) {
            if (syncRequest.userId.IsNull()) {
                user = null;
                type = PreAuthType.MissingUserId;
                return !allUsers.invalidated;
            }
            if (syncRequest.token.IsNull()) {
                user = null;
                type = PreAuthType.MissingToken;
                return !allUsers.invalidated;
            }
            if (users.TryGetValue(syncRequest.userId, out user)) {
                if (user.invalidated) {
                    type = PreAuthType.UserInvalidated;
                    return false;
                }
                if (!user.token.IsEqual(syncRequest.token)) {
                    type = PreAuthType.Failed;
                    return false;
                }
                type = PreAuthType.Success;
                return true;
            }
            type = PreAuthType.UserUnknown;
            return false;
        }
        
        private const string InvalidUserToken = "Authentication failed";
        
        private bool AuthenticateSynchronous(PreAuthType type, User user, SyncContext syncContext) {
            var all = allUsers;
            switch (type) {
                case PreAuthType.MissingUserId:
                    syncContext.AuthenticationFailed(anonymous, "user authentication requires 'user' id", all.taskAuthorizer, all.hubPermission);
                    return true;
                case PreAuthType.MissingToken:
                    syncContext.AuthenticationFailed(anonymous, "user authentication requires 'token'", all.taskAuthorizer, all.hubPermission);
                    return true;
                case PreAuthType.Failed:
                    syncContext.AuthenticationFailed(user, InvalidUserToken, all.taskAuthorizer, all.hubPermission);
                    return true;
                case PreAuthType.Success:
                    syncContext.AuthenticationSucceed(user, user.taskAuthorizer, user.hubPermission);
                    return true;
            }
            return false;
        }

        public override async Task AuthenticateAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            // Note: UserStore could be cached. This requires a FlioxClient.ClearEntities()
            // UserStore is not thread safe, create new one per Authenticate request.
            using (var pooled = syncContext.pool.Type(() => new UserStore (userHub)).Get()) {
                var userStore       = pooled.instance;
                userStore.UserId    = UserStore.AuthenticationUser;
                var type            = syncRequest.intern.preAuthType;
                var user            = syncRequest.intern.preAuthUser;
                var all             = allUsers;
                if (all.invalidated) {
                    await SetUserAuthAsync(all, userStore); // ensure anonymous is not invalidated
                }
                var ua = new UserAuthInfo();
                ua.AddUserAuth(all);
                switch (type) {
                    case PreAuthType.MissingUserId:
                    case PreAuthType.MissingToken:
                    case PreAuthType.Failed:
                        AuthenticateSynchronous(type, user, syncContext);
                        return;
                    case PreAuthType.UserUnknown:
                    case PreAuthType.UserInvalidated:
                        break;
                    default:
                        throw new InvalidOperationException($"unexpected PreAuthType type: {type}");
                }
                var auth        = userAuth ?? userStore;
                var userIdShort = syncRequest.userId;
                var userId      = userIdShort.AsString();
                var token       = syncRequest.token;
                var command     = new Credentials { userId = userId, token = token.AsString() };
                var result      = await auth.AuthenticateAsync(command).ConfigureAwait(false);
                
                // authentication failed?
                if (!result.isValid) {
                    users.TryAdd(anonymous.userId, anonymous);
                    syncContext.AuthenticationFailed(anonymous, InvalidUserToken, all.taskAuthorizer, all.hubPermission);
                    return;
                }
                var error = await GetUserAuthInfoAsync(userStore, userId, ua).ConfigureAwait(false);
                if (error != null) {
                    users.TryAdd(anonymous.userId, anonymous);
                    syncContext.AuthenticationFailed(anonymous, error, all.taskAuthorizer, all.hubPermission);
                    return;
                }
                var authenticated = authenticatedUsers;
                if (authenticated.invalidated) {
                    await SetUserAuthAsync(authenticated, userStore);
                }
                ua.AddUserAuth(authenticated);
                user ??= new User(userIdShort);
                user.Set(token, ua.GetTaskAuthorizer(), ua.GetHubPermission(), ua.GetRoles());
                user.SetGroups(ua.GetGroups());
                users.TryAdd(userIdShort, user);
                syncContext.AuthenticationSucceed(user, user.taskAuthorizer, user.hubPermission);
            }
        }
        
        public override ClientIdValidation ValidateClientId(ClientController clientController, SyncContext syncContext) {
            ref var clientId = ref syncContext.clientId; 
            if (clientId.IsNull()) {
                return ClientIdValidation.IsNull;
            }
            if (!syncContext.Authenticated) {
                return ClientIdValidation.Invalid;
            }
            var user        = syncContext.User;
            var userClients = user.clients; 
            if (userClients.ContainsKey(clientId)) {
                return ClientIdValidation.Valid;
            }
            /* // Is clientId already used?
            if (clientController.Clients.TryGetValue(clientId, out var userClient)) {
                if (!userClient.userId.IsEqual(user.userId))
                    return ClientIdValidation.Invalid;
            } */
            if (clientController.UseClientIdFor(user, clientId)) {
                userClients.TryAdd(clientId, new Empty());
                return ClientIdValidation.Valid;
            }
            return ClientIdValidation.Invalid;
        }
        
        public override bool EnsureValidClientId(ClientController clientController, SyncContext syncContext, out string error) {
            switch (syncContext.clientIdValidation) {
                case ClientIdValidation.Valid:
                    error = null;
                    return true;
                case ClientIdValidation.Invalid:
                    error = $"invalid client id. 'clt': {syncContext.clientId}";
                    return false;
                case ClientIdValidation.IsNull:
                    error           = null;
                    var user        = syncContext.User; 
                    var clientId    = clientController.NewClientIdFor(user);
                    syncContext.SetClientId(clientId);
                    return true;
            }
            throw new InvalidOperationException ("unexpected clientIdValidation state");
        }
        
        public override async Task SetUserOptionsAsync (User user, UserParam param) {
            var store       = new UserStore(userHub);
            store.UserId    = UserStore.AuthenticationUser;
            var userId      = user.userId.AsString();
            var read        = store.targets.Read().Find(userId);
            await store.SyncTasks().ConfigureAwait(false);
            
            var userTarget      = read.Result ?? new UserTarget { id = userId, groups = new List<string>() };
            var groups          = User.UpdateGroups(userTarget.groups, param);
            userTarget.groups   = groups.ToList();
            store.targets.Upsert(userTarget);
            await store.SyncTasks().ConfigureAwait(false);

            await base.SetUserOptionsAsync(user, param).ConfigureAwait(false); 
        }
        
        private async Task SetUserAuthAsync(User user, UserStore userStore) {
            var ua = new UserAuthInfo();
            var error   = await GetUserAuthInfoAsync(userStore, user.userId.AsString(), ua);
            if (error != null) {
                user.Set(default, TaskAuthorizer.None, HubPermission.None, null);
                return;
            }
            user.Set(default, ua.GetTaskAuthorizer(), ua.GetHubPermission(), ua.GetRoles());
        }

        private async Task<string> GetUserAuthInfoAsync(UserStore userStore, string userId, UserAuthInfo result) {
            var readPermission  = userStore.permissions.Read().Find(userId);
            var readTarget      = userStore.targets.Read().Find(userId);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            var targetGroups    = readTarget.Result?.groups;
            UserPermission permission = readPermission.Result;
            var roleNames = permission?.roles;
            if (roleNames == null || roleNames.Count == 0) {
                return null;
            }
            var error = await AddNewRoles(userStore, roleNames).ConfigureAwait(false);
            if (error != null)
                return error;
            
            foreach (var roleName in roleNames) {
                // existence is checked already in AddNewRoles()
                if (!roleCache.TryGetValue(roleName, out var role))
                    throw new InvalidOperationException($"roleAuthorizers not found: {roleName}");
                result.AddRole(role);
            }
            result.AddGroups(targetGroups);
            return null;
        }
        
        private async Task<string> AddNewRoles(UserStore userStore, List<string> roles) {
            var newRoles = new List<string>();
            foreach (var role in roles) {
                if (!roleCache.TryGetValue(role, out _)) {
                    newRoles.Add(role);
                }
            }
            if (newRoles.Count == 0)
                return null;
            var readRoles = userStore.roles.Read().FindRange(newRoles);
            await userStore.SyncTasks().ConfigureAwait(false);
            
            foreach (var newRolePair in readRoles.Result) {
                string role     = newRolePair.Key;
                Role newRole    = newRolePair.Value;
                if (newRole == null)
                    return $"authorization role not found: '{role}'";
                var taskAuthorizers = new List<TaskAuthorizer>(newRole.taskRights.Count);
                foreach (var taskRight in newRole.taskRights) {
                    TaskAuthorizer authorizer;
                    if (taskRight is PredicateRight predicates) {
                        var result = GetPredicatesAuthorizer(predicates);
                        if (!result.Success)
                            return $"{result.error} in role: {newRole.id}";
                        authorizer = result.value;
                    } else {
                        authorizer = taskRight.ToAuthorizer();
                    }
                    taskAuthorizers.Add(authorizer);
                }
                var hubPermission   = new HubPermission (newRole.hubRights?.queueEvents == true);
                var roleRights      = new UserAuthRole (role, taskAuthorizers.ToArray(), hubPermission);
                roleCache.TryAdd(role, roleRights);
            }
            return null;
        }
        
        private Result<TaskAuthorizer> GetPredicatesAuthorizer(PredicateRight right) {
            var authorizers = new List<TaskAuthorizer>(right.names.Count);
            foreach (var predicateName in right.names) {
                if (!registeredPredicates.TryGetValue(predicateName, out var predicate)) {
                    return $"unknown predicate: {predicateName}";
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