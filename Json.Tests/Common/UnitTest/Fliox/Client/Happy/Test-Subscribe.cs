﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Threading;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

// ReSharper disable ConvertToLambdaExpression
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    internal enum EventAssertion {
        /// <summary>Assert a <see cref="Friflo.Json.Fliox.Hub.Client.SubscriptionProcessor"/> will not get change events from the
        /// <see cref="FlioxClient"/> it is attached to.</summary>.
        NoChanges,
        /// <summary>Assert a <see cref="Friflo.Json.Fliox.Hub.Client.SubscriptionProcessor"/> will get change events from all
        /// <see cref="FlioxClient"/>'s it is not attached to.</summary>.
        Changes
    }
    
    public partial class TestHappy
    {
        [UnityTest] public IEnumerator  SubscribeCoroutine() { yield return RunAsync.Await(AssertSubscribe()); }
        [Test]      public void         SubscribeSync() { SingleThreadSynchronizationContext.Run(AssertSubscribe); }
        
        private static async Task AssertSubscribe() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(false))
            using (var database         = new FileDatabase(TestGlobals.DB, TestGlobals.PocStoreFolder, new PocHandler()))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var listenDb         = new PocStore(hub) { UserId = "listenDb", ClientId = "listen-client" }) {
                hub.EventDispatcher     = eventDispatcher;
                var listenSubscriber    = await CreatePocStoreSubscriber(listenDb, EventAssertion.Changes);
                using (var createStore  = new PocStore(hub) { UserId = "createStore", ClientId = "create-client"}) {
                    var createSubscriber = await CreatePocStoreSubscriber(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenSubscriber.receivedAll ) { await Task.Delay(1); }

                    AreEqual(1, createSubscriber.EventSequence);  // received no change events for changes done by itself
                }
                listenSubscriber.AssertCreateStoreChanges();
                AreEqual(9, listenSubscriber.EventSequence);           // non protected access
                await eventDispatcher.FinishQueues();
            }
        }
        
        private static async Task<PocStoreSubscriber> CreatePocStoreSubscriber (PocStore store, EventAssertion eventAssertion) {
            var subscriber = new PocStoreSubscriber(store, eventAssertion);
            store.SetEventProcessor(new SynchronizationContextProcessor());
            store.SubscriptionEventHandler += subscriber.OnEvent;
            
            var subscriptions   = store.SubscribeAllChanges(ChangeFlags.All, context => {
                AreEqual("createStore", context.SrcUserId.AsString());
                foreach (var changes in context.Changes) {
                    subscriber.countAllChanges += changes.Count;
                }
            });
            // change subscription of specific EntitySet<Article>
            var articlesSub         = store.articles.SubscribeChanges(ChangeFlags.All, (changes, context) => { });
            
            var subscribeMessage    = store.SubscribeMessage(TestRelationPoC.EndCreate, (msg, context) => {
                AreEqual("EndCreate (param: null)", msg.ToString());
                subscriber.receivedAll = true;
                IsTrue(                     msg.RawParam.IsNull());
                AreEqual("null",            msg.RawParam.AsString());
            });
            var subscribeMessage1   = store.SubscribeMessage<TestCommand>(nameof(TestCommand), (msg, context) => {
                AreEqual(@"TestCommand (param: {""text"":""test message""})", msg.ToString());
                subscriber.testMessageCalls++;
                msg.GetParam(out TestCommand param, out _);
                AreEqual("test message",        param.text);
                AreEqual(nameof(TestCommand),   msg.Name);
            });
            var subscribeMessage2   = store.SubscribeMessage<int>(TestRelationPoC.TestMessageInt, (msg, context) => {
                subscriber.testMessageIntCalls++;
                msg.GetParam(out int param, out _);
                AreEqual(42,                            param);
                AreEqual("42",                          msg.RawParam.AsString());
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.GetParam(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.GetParam<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error);
                
                msg.GetParam<string>(out _, out string error2);
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error2);
            });
            var subscribeMessage3   = store.SubscribeMessage(TestRelationPoC.TestMessageInt, (msg, context) => {
                subscriber.testMessageIntCalls++;
                msg.GetParam(out int val, out _);
                AreEqual(42,                            val);
                AreEqual("42",                          msg.RawParam.AsString());
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.GetParam(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.GetParam<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error);
                
                msg.GetParam<string>(out _, out string error2);
                AreEqual("JsonReader/error: Cannot assign number to string. got: 42 path: '(root)' at position: 2", error2);
            });
            
            var subscribeMessage4   = store.SubscribeMessage  (TestRelationPoC.TestRemoveHandler, RemovedHandler);
            var unsubscribe1        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveHandler, RemovedHandler);

            var subscribeMessage5   = store.SubscribeMessage  (TestRelationPoC.TestRemoveAllHandler, RemovedHandler);
            var unsubscribe2        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveAllHandler, null);
            
            var subscribeAllMessages= store.SubscribeMessage  ("Test*", (msg, context) => {
                subscriber.testWildcardCalls++;
            });

            await store.SyncTasks(); // ----------------

            foreach (var subscription in subscriptions) {
                IsTrue(subscription.Success);    
            }
            IsTrue(articlesSub.Success);
                
            IsTrue(subscribeMessage.        Success);
            IsTrue(subscribeMessage1.       Success);
            IsTrue(subscribeMessage2.       Success);
            IsTrue(subscribeMessage3.       Success);
            IsTrue(subscribeMessage4.       Success);
            IsTrue(subscribeMessage5.       Success);
            IsTrue(unsubscribe1.            Success);
            IsTrue(unsubscribe2.            Success);
            IsTrue(subscribeAllMessages.    Success);
            return subscriber;
        }
        
        private static readonly MessageSubscriptionHandler<int> RemovedHandler = (msg, context) => {
            Fail("unexpected call");
        };
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocStoreSubscriber {
        private readonly    PocStore        client;
        private             ChangeInfo      orderSum;
        private             ChangeInfo      customerSum;
        private             ChangeInfo      articleSum;
        private             ChangeInfo      producerSum;
        private             ChangeInfo      employeeSum;
        private             ChangeInfo      typesSum;
        private             int             messageCount;
        internal            int             testMessageCalls;
        internal            int             testMessageIntCalls;
        internal            int             testWildcardCalls;
        internal            int             subscribeEventsCalls;
        internal            bool            receivedAll;
        internal            int             countAllChanges;
        internal            int             EventSequence { get; private set; }
        
        private readonly    EventAssertion  eventAssertion;
        
        internal PocStoreSubscriber (PocStore client, EventAssertion eventAssertion) {
            this.client         = client;
            this.eventAssertion = eventAssertion;
        }
            
        /// All tests using <see cref="PocStoreSubscriber"/> are required to use "createStore" as userId
        public void OnEvent (EventContext context) {
            AreEqual("createStore", context.SrcUserId.ToString());
            EventSequence = context.EventSequence;
            
            context.ApplyChangesTo(client);
            
            CheckSomeMessages(context);
            
            var orderChanges    = context.GetChanges(client.orders);
            var customerChanges = context.GetChanges(client.customers);
            var articleChanges  = context.GetChanges(client.articles);
            var producerChanges = context.GetChanges(client.producers);
            var employeeChanges = context.GetChanges(client.employees);
            var typesChanges    = context.GetChanges(client.types);
            var messages        = context.Messages;
            
            orderSum.   Add(orderChanges.ChangeInfo);
            customerSum.Add(customerChanges.ChangeInfo);
            articleSum. Add(articleChanges.ChangeInfo);
            producerSum.Add(producerChanges.ChangeInfo);
            employeeSum.Add(employeeChanges.ChangeInfo);
            typesSum.   Add(typesChanges.ChangeInfo);
            messageCount += messages.Count;

            foreach (var message in messages) {
                switch (message.Name) {
                    case nameof(TestRelationPoC.EndCreate):
                        IsTrue  (        message.RawParam.IsNull());
                        AreEqual("null", message.RawParam.AsString());
                        break;
                    case nameof(TestRelationPoC.TestMessageInt):
                        message.GetParam(out int intVal, out _);
                        AreEqual(42, intVal);
                        break;
                    case nameof(TestRelationPoC.TestRemoveHandler):
                    case nameof(TestRelationPoC.TestRemoveAllHandler):
                        break;
                    case nameof(TestCommand):
                        message.GetParam(out TestCommand testVal, out _);
                        AreEqual("test message", testVal.text);
                        break;
                    default:
                        Fail("test expect handling all messages");
                        break;
                }
            }
            
            switch (eventAssertion) {
                case EventAssertion.NoChanges:
                    var changeInfo = context.Changes;
                    IsTrue(changeInfo.Count == 0);
                    break;
                case EventAssertion.Changes:
                    changeInfo = context.Changes;
                    IsTrue(changeInfo.Count > 0);
                    AssertChangeEvent(articleChanges);
                    break;
            }
        }
        
        private void CheckSomeMessages(EventContext context) {
            subscribeEventsCalls++;
            var eventInfo = context.EventInfo;
            switch (context.EventSequence) {
                case 3:
                    AreEqual(6, eventInfo.Count);
                    AreEqual(6, eventInfo.changes.Count);
                    AreEqual("creates: 2, upserts: 4, deletes: 0, patches: 0, messages: 0", eventInfo.ToString());
                    var articleChanges  = context.GetChanges(client.articles);
                    var producerChanges = context.GetChanges(client.producers);
                    AreEqual(1, articleChanges.Creates.Count);
                    AreEqual("articles - creates: 1, upserts: 4, deletes: 0, patches: 0", articleChanges.ToString());
                    AreEqual("producers - creates: 1, upserts: 0, deletes: 0, patches: 0", producerChanges.ToString());
                    break;
                case 9:
                    AreEqual(6, eventInfo.Count);
                    AreEqual(5, eventInfo.messages);
                    AreEqual(1, eventInfo.changes.upserts);
                    var messages = context.Messages;
                    AreEqual(5, messages.Count);
                    break;
            }
        }
        
        private  void AssertChangeEvent (Changes<string, Article> articleChanges) {
            switch (EventSequence) {
                case 2:
                    AreEqual("creates: 0, upserts: 2, deletes: 0, patches: 0", articleChanges.ChangeInfo.ToString());
                    var ipad = articleChanges.Upserts.Find(e => e.id == "article-ipad");
                    AreEqual("iPad Pro", ipad.name);
                    break;
                case 5:
                    AreEqual("creates: 0, upserts: 0, deletes: 1, patches: 4", articleChanges.ChangeInfo.ToString());
                    IsTrue(articleChanges.Deletes.Contains("article-delete"));
                    Patch<string> articlePatch = articleChanges.Patches.Find(p => p.key == "article-1");
                    AreEqual("article-1",               articlePatch.ToString());
                    var articlePatch0 = (PatchReplace)  articlePatch.patches[0];
                    AreEqual("/name",                   articlePatch0.path);
                    AreEqual("\"Changed name\"",        articlePatch0.value.AsString());
                    
                    // cached article is updated by ApplyChangesTo()
                    client.articles.Local.TryGetValue("article-1", out var article);
                    AreEqual("Changed name",            article.name);
                    break;
            }
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.CreateStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(9, EventSequence);
            AreEqual(9, subscribeEventsCalls);
            
            AreSimilar("creates: 0, upserts: 2, deletes: 0, patches: 0", orderSum);
            AreSimilar("creates: 1, upserts: 6, deletes: 1, patches: 0", customerSum);
            AreSimilar("creates: 2, upserts: 7, deletes: 5, patches: 6", articleSum);
            AreSimilar("creates: 3, upserts: 0, deletes: 3, patches: 0", producerSum);
            AreSimilar("creates: 1, upserts: 0, deletes: 1, patches: 0", employeeSum);
            AreSimilar("creates: 0, upserts: 1, deletes: 0, patches: 0", typesSum);
            
            AreEqual(5,  messageCount);
            AreEqual(4,  testWildcardCalls);

            AreEqual(1,  testMessageCalls);
            AreEqual(2,  testMessageIntCalls);
            
            var allChanges = orderSum.Count + customerSum.Count + articleSum.Count + producerSum.Count + employeeSum.Count + typesSum.Count;
            
            AreEqual(39, allChanges);
            AreEqual(39, countAllChanges);
        }
    }
    
    public partial class TestHappy {
        [Test]
        public void AcknowledgeMessages() { SingleThreadSynchronizationContext.Run(AssertAcknowledgeMessages); }
            
        private static async Task AssertAcknowledgeMessages() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(false))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var listenDb         = new FlioxClient(hub) { ClientId = "listenDb" }) {
                listenDb.SetEventProcessor(new SynchronizationContextProcessor());
                hub.EventDispatcher = eventDispatcher;
                bool receivedHello = false;
                listenDb.SubscribeMessage("Hello", (msg, context) => {
                    receivedHello = true;
                });
                await listenDb.SyncTasks();

                using (var sendStore  = new FlioxClient(hub) { ClientId = "sendStore" }) {
                    sendStore.SendMessage("Hello", "some text");
                    await sendStore.SyncTasks();
                    
                    while (!receivedHello) {
                        await Task.Delay(1); // release thread to process message event handler
                    }
                    
                    await listenDb.SyncTasks();

                    // assert no send events are pending which are not acknowledged
                    AreEqual(0, eventDispatcher.NotAcknowledgedEvents());
                }
            }
        }
        
        [Test]
        public void MultiDbSubscriptions() { SingleThreadSynchronizationContext.Run(AssertMultiDbSubscriptions); }
            
        private static async Task AssertMultiDbSubscriptions() {
            using (var _                = SharedEnv.Default) // for LeakTestsFixture
            using (var eventDispatcher  = new EventDispatcher(false))
            using (var database         = new MemoryDatabase(TestGlobals.DB))
            using (var extDB            = new MemoryDatabase("ext_db"))
            using (var hub              = new FlioxHub(database, TestGlobals.Shared))
            using (var listenMainDb     = new FlioxClient(hub)              { ClientId = "listenMainDb" })
            using (var listenExtDb      = new FlioxClient(hub, "ext_db")    { ClientId = "listenExtDb" }) {
                hub.EventDispatcher = eventDispatcher;
                hub.AddExtensionDB(extDB);
                int receivedHelloMainDB = 0;
                int receivedHelloExtDB  = 0;
                listenMainDb.SubscribeMessage("hello-main_db", (msg, context) => {
                    receivedHelloMainDB++;
                });
                listenMainDb.SubscribeMessage("hello-ext_db", (msg, context) => {
                    throw new InvalidOperationException("expect only main_db messages");
                });
                listenExtDb.SubscribeMessage("hello-main_db", (msg, context) => {
                    throw new InvalidOperationException("expect only ext_db messages");
                });
                listenExtDb.SubscribeMessage("hello-ext_db", (msg, context) => {
                    receivedHelloExtDB++;
                });
                
                await listenMainDb.SyncTasks();
                await listenExtDb.SyncTasks();
                

                using (var mainDbStore  = new FlioxClient(hub)              { ClientId = "mainDbStore" })
                using (var extDbStore   = new FlioxClient(hub, "ext_db")    { ClientId = "extDbStore" })
                {
                    extDbStore.SendMessage("hello-ext_db", "some text");
                    await extDbStore.SyncTasks();
                    
                    mainDbStore.SendMessage("hello-main_db", "some text to ext_db");
                    await mainDbStore.SyncTasks();
                    
                    while (receivedHelloMainDB == 0) {
                        await Task.Delay(1); // release thread to process message event handler
                    }
                    AreEqual(1, receivedHelloMainDB);
                    AreEqual(1, receivedHelloExtDB);
                    
                    await listenMainDb.SyncTasks();
                    await listenExtDb.SyncTasks();

                    // assert no send events are pending which are not acknowledged
                    AreEqual(0, eventDispatcher.NotAcknowledgedEvents());
                }
            }
        }
    }

}