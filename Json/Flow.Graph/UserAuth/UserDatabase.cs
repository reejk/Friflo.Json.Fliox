﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Utils;

namespace Friflo.Json.Flow.UserAuth
{
    /// <summary>
    /// Used to authenticate users stored in the given user <see cref="EntityDatabase"/>.
    /// If user authentication succeed it returns also the roles attached to a user. 
    /// The schema of the user database is defined in <see cref="UserStore"/>.
    /// </summary>
    public class UserDatabase : IDisposable
    {
        private readonly SharedPool<UserStore>   storePool;
        
        public UserDatabase(EntityDatabase authDatabase) {
            storePool = new SharedPool<UserStore>      (() => new UserStore(authDatabase, UserStore.Server));
            authDatabase.authenticator = new UserDatabaseAuthenticator();
            authDatabase.taskHandler.AddCommandHandlerAsync<AuthenticateUser, AuthenticateUserResult>(AuthenticateUserHandler); 
        }
        
        public void Dispose() {
            storePool.Dispose();
        }
        
        private async Task<AuthenticateUserResult> AuthenticateUserHandler (Command<AuthenticateUser> command) {
            using (var pooledStore = storePool.Get()) {
                var store = pooledStore.instance;
                var validateToken   = command.Value;
                var clientId        = validateToken.clientId;
                var readCredentials = store.credentials.Read();
                var findCred        = readCredentials.Find(clientId);
                var readRoles       = store.roles.Read();
                var findRole        = readRoles.Find(clientId);
                
                await store.Sync();

                UserCredential  cred = findCred.Result;
                UserRole        role = findRole.Result;
                bool            isValid = cred != null && cred.token == validateToken.token;
                return new AuthenticateUserResult { isValid = isValid, roles = role?.roles };
            }
        }
    }
}