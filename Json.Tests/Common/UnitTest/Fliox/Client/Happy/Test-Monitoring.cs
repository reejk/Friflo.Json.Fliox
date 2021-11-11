// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Auth;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Monitor;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.UserAuth;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    public partial class TestStore
    {
        private static readonly string HostName  = "Test";
        
        [Test]
        public static async Task TestMonitoringFile() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared, HostName))
            using (var monitorDB        = new MonitorDatabase(hub)) {
                hub.AddExtensionDB(monitorDB);
                await AssertNoAuthMonitoringDB  (hub);
                await AssertAuthMonitoringDB    (hub, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringLoopback() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var database         = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          	= new FlioxHub(database, TestGlobals.Shared, HostName))
            using (var monitor          = new MonitorDatabase(hub))
            using (var loopbackHub      = new LoopbackHub(hub)) {
                hub.AddExtensionDB(monitor);
                await AssertNoAuthMonitoringDB  (loopbackHub);
                await AssertAuthMonitoringDB    (loopbackHub, hub);
            }
        }
        
        [Test]
        public static async Task TestMonitoringHttp() {
            using (var _            = SharedEnv.Default) // for LeakTestsFixture
            using (var database     = new FileDatabase(TestGlobals.PocStoreFolder))
            using (var hub          = new FlioxHub(database, TestGlobals.Shared, HostName))
            using (var hostHub      = new HttpHostHub(hub))
            using (var server       = new HttpListenerHost("http://+:8080/", hostHub)) 
            using (var monitor      = new MonitorDatabase(hub))
            using (var clientHub    = new HttpClientHub("http://localhost:8080/", TestGlobals.Shared)) {
                hub.AddExtensionDB(monitor);
                await RunServer(server, async () => {
                    await AssertNoAuthMonitoringDB  (clientHub);
                    await AssertAuthMonitoringDB    (clientHub, hub);
                });
            }
        }
        
        private static async Task AssertAuthMonitoringDB(FlioxHub hub, FlioxHub database) {
            using (var userDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/UserStore", new UserDBHandler()))
            using (var authenticator    = new UserAuthenticator(userDatabase, TestGlobals.Shared)) {
                database.Authenticator  = authenticator;
                await AssertAuthSuccessMonitoringDB (hub);
                await AssertAuthFailedMonitoringDB  (hub);
            }
        }

        private  static async Task AssertNoAuthMonitoringDB(FlioxHub hub) {
            const string userId     = "poc-user";
            const string clientId   = "poc-client"; 
            const string token      = "invalid"; 
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, "monitor")) {
                var result = await Monitor(store, monitor, userId, clientId, token);
                AssertNoAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId, token);
                AssertNoAuthResult(result);
            }
        }

        private  static async Task AssertAuthSuccessMonitoringDB(FlioxHub hub) {
            const string userId     = "admin";
            const string clientId   = "admin-client";
            const string token      = "admin-token";
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, "monitor")) {
                var result = await Monitor(store, monitor, userId, clientId, token);
                AssertAuthResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId, token);
                AssertAuthResult(result);
                
                await AssertMonitorErrors(monitor);
            }
        }
        
        private  static async Task AssertAuthFailedMonitoringDB(FlioxHub hub) {
            const string userId     = "admin";
            const string clientId   = "admin-xxx"; 
            const string token      = "invalid";
            using (var store    = new PocStore(hub))
            using (var monitor  = new MonitorStore(hub, "monitor")) {
                var result = await Monitor(store, monitor, userId, clientId, token);
                AssertAuthFailedResult(result);
                
                // as clearing monitor stats subsequent call has same result
                result = await Monitor(store, monitor, userId, clientId, token);
                AssertAuthFailedResult(result);
            }
        }

        private static void AssertNoAuthResult(MonitorResult result) {
            IsFalse(result.users.Success);
            IsFalse(result.clients.Success);
            IsFalse(result.hosts.Success);
            IsFalse(result.user.Success);
            IsFalse(result.client.Success);
        }

        private static void AssertAuthResult(MonitorResult result) {
            var users   = result.users.Results;
            var clients = result.clients.Results;
            var host    = result.hosts.Results[new JsonKey("Test")];
            AreEqual("{'id':'Test','counts':{'requests':2,'tasks':3}}",                                      host.ToString());
            AreEqual("{'id':'anonymous','clients':[],'counts':[]}",                                          users[User.AnonymousId].ToString());
            
            var adminInfo = users[new JsonKey("admin")].ToString();
            AreEqual("{'id':'admin','clients':['admin-client'],'counts':[{'db':'default','requests':1,'tasks':2},{'db':'monitor','requests':1,'tasks':1}]}", adminInfo);
                
            var adminClientInfo = clients[new JsonKey("admin-client")].ToString();
            AreEqual("{'id':'admin-client','user':'admin','counts':[{'db':'default','requests':1,'tasks':2}]}", adminClientInfo);
            var monitorClientInfo = clients[new JsonKey("monitor-client")].ToString();
            AreEqual("{'id':'monitor-client','user':'admin','counts':[{'db':'monitor','requests':1,'tasks':1}]}", monitorClientInfo);
            
            NotNull(result.user.Result);
            NotNull(result.client.Result);
        }
        
        private static void AssertAuthFailedResult(MonitorResult result) {
            IsFalse(result.users.Success);
            IsFalse(result.clients.Success);
        }

        private  static async Task AssertMonitorErrors(MonitorStore monitor) {
            var deleteUser      = monitor.users.Delete(new JsonKey("123"));
            var createUser      = monitor.users.Create(new UserInfo{id = new JsonKey("abc")});
            await monitor.TrySyncTasks();
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'create'",   createUser.Error.Message);
            AreEqual("InvalidTask ~ MonitorDatabase does not support task: 'delete'",   deleteUser.Error.Message);
        }
        
        private  static async Task<MonitorResult> Monitor(PocStore store, MonitorStore monitor, string userId, string clientId, string token) {
            monitor.ClientId    = "monitor-client";
            // clear stats requires successful authentication as admin
            monitor.UserId      = "admin";
            monitor.Token       = "admin-token";
            monitor.ClearStats();
            await monitor.TrySyncTasks();
            
            store.UserId        = userId;
            store.ClientId      = clientId;
            store.Token         = token;

            store.articles.Read().Find("xxx");
            store.customers.Read().Find("yyy");
            await store.TrySyncTasks();
            
            monitor.UserId      = userId;
            monitor.Token       = token;
            
            var result = new MonitorResult {
                users       = monitor.users.QueryAll(),
                clients     = monitor.clients.QueryAll(),
                user        = monitor.users.Read().Find(new JsonKey(userId)),
                client      = monitor.clients.Read().Find(new JsonKey(clientId)),
                hosts       = monitor.hosts.QueryAll(),
                sync        = await monitor.TrySyncTasks()
            };
            return result;
        }
        
        internal class MonitorResult {
            internal    SyncResult                      sync;
            internal    QueryTask<JsonKey,  UserInfo>   users;
            internal    QueryTask<JsonKey,  ClientInfo> clients;
            internal    Find<JsonKey,       UserInfo>   user;
            internal    Find<JsonKey,       ClientInfo> client;
            internal    QueryTask<JsonKey,  HostInfo>   hosts;
        }
    }
}
