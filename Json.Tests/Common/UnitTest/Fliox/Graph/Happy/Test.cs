﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Graph;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.DB.NoSQL.Event;
using Friflo.Json.Fliox.DB.NoSQL.Remote;
using Friflo.Json.Fliox.DB.NoSQL.Utils;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph.Happy
{
    public partial class TestStore : LeakTestsFixture
    {
        /// withdraw from allocation detection by <see cref="LeakTestsFixture"/> => init before tracking starts
        [NUnit.Framework.OneTimeSetUp]    public static void  Init()       { TestGlobals.Init(); }
        [NUnit.Framework.OneTimeTearDown] public static void  Dispose()    { TestGlobals.Dispose(); }
        

        [UnityTest] public IEnumerator  CollectAwaitCoroutine() { yield return RunAsync.Await(CollectAwait(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task   CollectAwaitAsync() { await CollectAwait(); }
        
        private static async Task CollectAwait() {
            List<Task> tasks = new List<Task>();
            for (int n = 0; n < 1000; n++) {
                Task task = Task.Delay(1);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
        }

        [UnityTest] public IEnumerator  ChainAwaitCoroutine() { yield return RunAsync.Await(ChainAwait(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task   ChainAwaitAsync() { await ChainAwait(); }
        private static async Task ChainAwait() {
            for (int n = 0; n < 5; n++) {
                await Task.Delay(1);
            }
        }
        
        [UnityTest] public IEnumerator  MemoryCreateCoroutine() { yield return RunAsync.Await(MemoryCreate()); }
        [Test]      public async Task   MemoryCreateAsync() { await MemoryCreate(); }
        
        private static async Task MemoryCreate() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var database     = new MemoryDatabase())
            using (var createStore  = new PocStore(database, "createStore"))
            using (var useStore     = new PocStore(database, "useStore"))  {
                await TestRelationPoC.CreateStore(createStore);
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileCreateCoroutine() { yield return RunAsync.Await(FileCreate(), i => Logger.Info("--- " + i)); }
        [Test]      public async Task  FileCreateAsync() { await FileCreate(); }

        private static async Task FileCreate() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var createStore  = new PocStore(fileDatabase, "createStore"))
            using (var useStore     = new PocStore(fileDatabase, "useStore")) {
                await TestRelationPoC.CreateStore(createStore);
                await TestStores(createStore, useStore);
            }
        }
        
        [UnityTest] public IEnumerator FileUseCoroutine() { yield return RunAsync.Await(FileUse()); }
        [Test]      public async Task  FileUseAsync() { await FileUse(); }
        
        private static async Task FileUse() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var useStore     = new PocStore(fileDatabase, "useStore")) {
                await TestStores(useStore, useStore);
            }
        }
        

        
        [UnityTest] public IEnumerator HttpCreateCoroutine() { yield return RunAsync.Await(HttpCreate()); }
        [Test]      public async Task  HttpCreateAsync() { await HttpCreate(); }
        
        private static async Task HttpCreate() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/", null))
            using (var remoteDatabase   = new HttpClientDatabase("http://localhost:8080/")) {
                await RunRemoteHost(hostDatabase, async () => {
                    using (var createStore      = new PocStore(remoteDatabase, "createStore"))
                    using (var useStore         = new PocStore(remoteDatabase, "useStore")) {
                        await TestRelationPoC.CreateStore(createStore);
                        await TestStores(createStore, useStore);
                    }
                });
            }
        }
        
        // accepting WebSockets in Unity fails at IsWebSocketRequest. See: 
        // [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
        // [UnityTest] public IEnumerator WebSocketCreateCoroutine()   { yield return RunAsync.Await(WebSocketCreate()); }
        [Test]      public void  WebSocketCreateSync()       { SingleThreadSynchronizationContext.Run(WebSocketCreate); }
        
        /// This test ensure that a <see cref="WebSocketClientDatabase"/> behaves exactly like all other
        /// <see cref="EntityDatabase"/> implementations in this file.
        /// It also ensures that a single <see cref="WebSocketClientDatabase"/> instance can be used by multiple clients
        /// simultaneously. In this case three <see cref="PocStore"/> instances.
        private static async Task WebSocketCreate() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker(false))
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/", null))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/"))
            using (var listenDb         = new PocStore(remoteDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                await RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    var listenProcessor     = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                    using (var createStore  = new PocStore(remoteDatabase, "createStore"))
                    using (var useStore     = new PocStore(remoteDatabase, "useStore")) {
                        var createSubscriber = await CreateSubscriptionProcessor(createStore, EventAssertion.NoChanges);
                        await TestRelationPoC.CreateStore(createStore);
                        
                        while (!listenProcessor.receivedAll ) { await Task.Delay(1); }
                        
                        AreEqual(1, createSubscriber.EventSequence);  // received no change events for changes done by itself
                        listenProcessor.AssertCreateStoreChanges();
                        await TestStores(createStore, useStore);
                    }
                    await remoteDatabase.Close();
                });
                await eventBroker.FinishQueues();
            }
        }
        
        [Test]      public void  WebSocketReconnectSync()       { SingleThreadSynchronizationContext.Run(WebSocketReconnect); }
        
        /// Test WebSocket disconnect while having changes subscribed. Change events pushed by the database may not arrived at subscriber.
        /// To ensure all change events arrive at <see cref="SubscriptionProcessor"/> <see cref="SyncRequest.eventAck"/>
        /// is used to inform database about arrived events. All not acknowledged events are resent.
        private static async Task WebSocketReconnect() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker(true))
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var hostDatabase     = new HttpHostDatabase(fileDatabase, "http://+:8080/", null))
            using (var remoteDatabase   = new WebSocketClientDatabase("ws://localhost:8080/"))
            using (var listenDb         = new PocStore(remoteDatabase, "listenDb")) {
                hostDatabase.fakeOpenClosedSockets = true;
                fileDatabase.eventBroker = eventBroker;
                await RunRemoteHost(hostDatabase, async () => {
                    await remoteDatabase.Connect();
                    var listenProcessor    = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                    using (var createStore  = new PocStore(fileDatabase, "createStore")) {
                        await remoteDatabase.Close();
                        // all change events sent by createStore doesnt arrive at listenDb
                        await TestRelationPoC.CreateStore(createStore);
                        AreEqual(0, listenProcessor.EventSequence);
                        
                        // subscriber contains send events which are not acknowledged
                        foreach (var subscriber in eventBroker.GetSubscribers()) {
                            IsTrue(subscriber.SentEventsCount > 0);
                        }
                        
                        await remoteDatabase.Connect();
                        
                        AreEqual(0, listenDb.Tasks.Count);
                        await listenDb.Sync();  // an empty Sync() is sufficient initiate re-sending all not-received change events

                        while (!listenProcessor.receivedAll ) { await Task.Delay(1); }
                        
                        listenProcessor.AssertCreateStoreChanges();

                        await listenDb.Sync();  // all changes are received => state of store remains unchanged
                        
                        // subscriber contains NO send events which are not acknowledged
                        foreach (var subscriber in eventBroker.GetSubscribers()) {
                            AreEqual(0, subscriber.SentEventsCount);
                        }
                        listenProcessor.AssertCreateStoreChanges();
                    }
                    await remoteDatabase.Close();
                });
                await eventBroker.FinishQueues();
            }
        }
        
        [UnityTest] public IEnumerator  LoopbackUseCoroutine() { yield return RunAsync.Await(LoopbackUse()); }
        [Test]      public void         LoopbackUseSync()     { SingleThreadSynchronizationContext.Run(LoopbackUse); }
        
        private static async Task LoopbackUse() {
            using (var _                = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker      = new EventBroker(false))
            using (var fileDatabase     = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var loopbackDatabase = new LoopbackDatabase(fileDatabase))
            using (var listenDb         = new PocStore(fileDatabase, "listenDb")) {
                fileDatabase.eventBroker    = eventBroker;
                var listenProcessor        = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                using (var createStore      = new PocStore(loopbackDatabase, "createStore"))
                using (var useStore         = new PocStore(loopbackDatabase, "useStore")) {
                    var createSubscriber        = await CreateSubscriptionProcessor(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenProcessor.receivedAll ) { await Task.Delay(1); }
                    
                    AreEqual(1, createSubscriber.EventSequence);  // received no change events for changes done by itself
                    listenProcessor.AssertCreateStoreChanges();
                    await TestStores(createStore, useStore);
                }
                await eventBroker.FinishQueues();
            }
        }
        
        internal static async Task RunRemoteHost(HttpHostDatabase remoteHost, Func<Task> run) {
            remoteHost.Start();
            Task hostTask = null;
            try {
                hostTask = Task.Run(() => {
                    // await hostDatabase.HandleIncomingConnections();
                    remoteHost.Run();
                    // await Task.Delay(100); // test awaiting hostTask
                    Logger.Info("1. RemoteHost finished");
                });
                
                await run();
            } finally {
                await remoteHost.Stop();
                if (hostTask != null)
                    await hostTask;
                Logger.Info("2. awaited hostTask");
            }
        } 

        // ------------------------------------ test assertion methods ------------------------------------
        public static async Task TestStores(PocStore createStore, PocStore useStore) {
            await AssertRefAssignment   (useStore);
            await AssertWriteRead       (createStore);
            await AssertEntityIdentity  (createStore);
            await AssertQuery           (createStore);
            await AssertRead            (createStore);
        }
        

        private static async Task TestCreate(Func<PocStore, Task> test) {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var createStore  = new PocStore(fileDatabase, "createStore")) {
                await TestRelationPoC.CreateStore(createStore);
                await test(createStore);
            }
        }
        
        private static async Task TestUse(Func<PocStore, Task> test) {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/Graph/PocStore"))
            using (var createStore  = new PocStore(fileDatabase, "createStore")) {
                await test(createStore);
            }
        }
    }
}