// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.DB.UserAuth
{
    /// <summary>
    /// used to remove users from cached <see cref="Host.Auth.Authenticator.users"/> in case:
    /// - a user permission changes
    /// - a role assigned to a user changes
    /// </summary>
    internal class UserStoreSubscriber
    {
        private readonly UserAuthenticator userAuthenticator;
        
        internal UserStoreSubscriber(UserAuthenticator userAuthenticator) {
            this.userAuthenticator = userAuthenticator;
        }
            
        internal void CreateSubscriber(UserAuthenticator userAuthenticator, FlioxHub hub) {
            var changes     = Changes.All;
            var store       = new UserStore (hub, userAuthenticator.userHub.DatabaseName);
            // userAuthenticator.userHub.EventDispatcher = new EventDispatcher(true);
            store.UserId    = UserStore.AuthenticationUser;
            store.UserId    = "admin";
            store.Token     = "admin";
            store.ClientId  = "user_db_subscriber";
            store.SetEventProcessor(new DirectEventProcessor());
            store.credentials.SubscribeChanges  (changes, CredentialChange);
            store.permissions.SubscribeChanges  (changes, PermissionChange);
            store.roles.SubscribeChanges        (changes, RoleChange);
            store.SyncTasks().Wait();
        }
        
        private void CredentialChange(EntityChanges<JsonKey, UserCredential> change, EventContext context) {
            var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
            foreach (var entity in change.Upserts) { changedUsers.Add(entity.id); }
            foreach (var id     in change.Deletes) { changedUsers.Add(id); }
            foreach (var patch  in change.Patches) { changedUsers.Add(patch.key); }
                
            foreach (var changedUser in changedUsers) {
                userAuthenticator.users.TryRemove(changedUser, out _);
            }
        }
        
        private void PermissionChange(EntityChanges<JsonKey, UserPermission> change, EventContext context) {
            var changedUsers = new HashSet<JsonKey>(JsonKey.Equality);
            foreach (var entity in change.Upserts) { changedUsers.Add(entity.id); }
            foreach (var id     in change.Deletes) { changedUsers.Add(id); }
            foreach (var patch  in change.Patches) { changedUsers.Add(patch.key); }
                
            foreach (var changedUser in changedUsers) {
                userAuthenticator.users.TryRemove(changedUser, out _);
            }
        }
        
        private void RoleChange(EntityChanges<string, Role> change, EventContext context) {
            var changedRoles    = new HashSet<string>();
            
            foreach (var entity in change.Upserts) { changedRoles.Add(entity.id); }
            foreach (var id     in change.Deletes) { changedRoles.Add(id); }
            foreach (var patch  in change.Patches) { changedRoles.Add(patch.key); }
            
            foreach (var changedRole in changedRoles) {
                if (!userAuthenticator.roleUserCache.TryGetValue(changedRole, out var roleUser))
                    continue;
                foreach (var userId in roleUser.users) {
                    userAuthenticator.users.TryRemove(userId, out _);    
                }
            }
        }
    }
    
    internal readonly struct RoleUsers
    {
        internal readonly HashSet<JsonKey> users;
        
        internal RoleUsers(HashSet<JsonKey> users) {
            this.users = users;
        }
    }
}