﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Utils;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public static void TestAuth () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/auth"))
                    using (var userAuth         = new UserAuth              (userDatabase))
                    using (                       new UserDatabaseHandler   (userDatabase))
                    using (var database         = new MemoryDatabase()) {
                        database.authenticator  = new UserAuthenticator(userAuth);
                        await AssertAuth(database);
                    }
                });
            }
        }
        
        [Test] public static void TestAuthAccess () {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            {
                SingleThreadSynchronizationContext.Run(async () => {
                    using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets/auth"))
                    using (var serverStore      = new UserStore             (userDatabase, UserStore.Server))
                    using (var authUserStore    = new UserStore             (userDatabase, UserStore.AuthUser))
                    using (                       new UserDatabaseHandler   (userDatabase)) {
                        // assert access to user database with different users: "Server" & "AuthUser"
                        await AssertNotAuthorized   (serverStore);
                        await AssertNotAuthorized   (authUserStore);
                        await AssertServerStore     (serverStore);
                        await AssertAuthUserStore   (authUserStore);
                    }
                });
            }
        }
        
        private static async Task AssertNotAuthorized(UserStore store) {
            var allCredentials  = store.credentials.QueryAll();
            var createTask      = store.credentials.Create(new UserCredential{ id="create-id" });
            var updateTask      = store.credentials.Update(new UserCredential{ id="update-id" });
            await store.TrySync();
            
            AreEqual("PermissionDenied ~ not authorized", allCredentials.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", createTask.Error.Message);
            AreEqual("PermissionDenied ~ not authorized", updateTask.Error.Message);
        }
        
        private static async Task AssertServerStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("user-mutate");
            await store.TrySync();
            
            var cred = credTask.Result;
            AreEqual("user-mutate-token", cred.token);
        }
        
        private static async Task AssertAuthUserStore(UserStore store) {
            var credTask        = store.credentials.Read().Find("user-mutate");
            await store.TrySync();
            
            AreEqual("PermissionDenied ~ not authorized", credTask.Error.Message);
        }

        private static async Task AssertAuth(EntityDatabase database) {
            using (var nullUser         = new PocStore(database, null))
            using (var unknownUser      = new PocStore(database, "unknown"))
            using (var mutateUser       = new PocStore(database, "user-mutate"))
            using (var readOnlyUser     = new PocStore(database, "user-readOnly"))
            {
                Tasks tasks;
                var newArticle = new Article{ id="new-article" };
                
                // test: clientId == null
                tasks = new Tasks(nullUser, newArticle);
                var sync = await nullUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires clientId)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires clientId)", tasks.updateArticles.Error.Message);
                
                // test: token ==  null
                unknownUser.SetToken(null);
                tasks = new Tasks(unknownUser, newArticle);
                sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires token)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (user authentication requires token)", tasks.updateArticles.Error.Message);
                
                // test: invalid token 
                unknownUser.SetToken("some token");
                tasks = new Tasks(unknownUser, newArticle);
                sync = await unknownUser.TrySync();
                AreEqual(2, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized (invalid user token)", tasks.findArticle.Error.Message);
                AreEqual("PermissionDenied ~ not authorized (invalid user token)", tasks.updateArticles.Error.Message);
                
                // test: allow readOnly & mutate 
                mutateUser.SetToken("user-mutate-token");
                tasks = new Tasks(mutateUser, newArticle);
                sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                IsTrue(tasks.Success);
                
                // test: store already authorized 
                tasks = new Tasks(mutateUser, newArticle);
                sync = await mutateUser.TrySync();
                AreEqual(0, sync.failed.Count);
                IsTrue(tasks.Success);

                // test: allow read only
                readOnlyUser.SetToken("user-readOnly-token");
                tasks = new Tasks(readOnlyUser, newArticle);
                sync = await readOnlyUser.TrySync();
                AreEqual(1, sync.failed.Count);
                AreEqual("PermissionDenied ~ not authorized", tasks.updateArticles.Error.Message);
            }
        }
        
        
        public class Tasks {
            public  ReadTask<Article>       readArticles;
            public  Find<Article>           findArticle;
            public  UpdateTask<Article>     updateArticles;
            
            public Tasks (PocStore store, Article newArticle) {
                readArticles    = store.articles.Read();
                findArticle     = readArticles.Find("some-id");
                updateArticles  = store.articles.Update(newArticle);
            }
            
            public bool Success => findArticle.Success && updateArticles.Success;
        }
    }
}
