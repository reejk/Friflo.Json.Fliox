﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Tests.Common.Utils;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

#if UNITY_5_3_OR_NEWER
    using UnitTest.Dummy;
#else
    using NUnit.Framework;
#endif

// ReSharper disable JoinDeclarationAndInitializer
namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph.Happy
{
    public partial class TestStore
    {
        [Test] public async Task TestSubscribe      () { await TestCreate(async (store) => await AssertSubscribe ()); }
        
        private static async Task AssertSubscribe() {
            using (var _            = Pools.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker())
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets/db"))
            using (var listenDb     = new PocStore(fileDatabase, "listenDb")) {
                fileDatabase.eventBroker = eventBroker;
                var pocSubscriber        = await CreatePocSubscriber(listenDb);
                using (var createStore = new PocStore(fileDatabase, "createStore")) {
                    var createSubscriber = await TestRelationPoC.SubscribeChanges(createStore);
                    await TestRelationPoC.FillStore(createStore);
                    AreEqual(0, createSubscriber.ChangeCount);  // received no change events for changes done by itself
                }
                pocSubscriber.AssertCreateStoreChanges();
                AreEqual(8, pocSubscriber.ChangeCount);           // non protected access
                AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 2)",  pocSubscriber.GetChangeInfo<Article>());  // non protected access
            }
        }
        
        private static async Task<PocSubscriber> CreatePocSubscriber (PocStore store) {
            var subscriber = new PocSubscriber();
            store.SetChangeSubscriber(subscriber);
            
            var changes = new HashSet<Change>(new [] {Change.create, Change.update, Change.delete, Change.patch});
            var subscriptions = store.SubscribeAll(changes);
                
            await store.Sync(); // -------- Sync --------

            foreach (var subscription in subscriptions) {
                IsTrue(subscription.Success);    
            }
            return subscriber;
        }
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocSubscriber : ChangeSubscriber {
        private readonly    ChangeInfo<Order>       orderSum     = new ChangeInfo<Order>();
        private readonly    ChangeInfo<Customer>    customerSum  = new ChangeInfo<Customer>();
        private readonly    ChangeInfo<Article>     articleSum   = new ChangeInfo<Article>();
        private readonly    ChangeInfo<Producer>    producerSum  = new ChangeInfo<Producer>();
        private readonly    ChangeInfo<Employee>    employeeSum  = new ChangeInfo<Employee>();
            
        public override void OnChanges (ChangesEvent changes, EntityStore store) {
            base.OnChanges(changes, store);
            var orderChanges    = GetEntityChanges<Order>();
            var customerChanges = GetEntityChanges<Customer>();
            var articleChanges  = GetEntityChanges<Article>();
            var producerChanges = GetEntityChanges<Producer>();
            var employeeChanges = GetEntityChanges<Employee>();
            
            var changeInfo = changes.GetChangeInfo();
            IsTrue(changeInfo.Count > 0);

            orderSum.   AddChanges(orderChanges);
            customerSum.AddChanges(customerChanges);
            articleSum. AddChanges(articleChanges);
            producerSum.AddChanges(producerChanges);
            employeeSum.AddChanges(employeeChanges);
            
            if (ChangeCount == 1) {
                AreEqual("iPad Pro", articleChanges.creates["article-ipad"].name);
                IsTrue(articleChanges.deletes.Contains("article-iphone"));
            }
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.FillStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(8,  ChangeCount);
            AreSimilar("(creates: 2, updates: 0, deletes: 0, patches: 0)", orderSum);
            AreSimilar("(creates: 6, updates: 0, deletes: 0, patches: 0)", customerSum);
            AreSimilar("(creates: 9, updates: 0, deletes: 4, patches: 0)", articleSum); // todo patches
            AreSimilar("(creates: 3, updates: 0, deletes: 0, patches: 0)", producerSum);
            AreSimilar("(creates: 1, updates: 0, deletes: 0, patches: 0)", employeeSum);
            
            IsTrue(orderSum      .IsEqual(GetChangeInfo<Order>()));
            IsTrue(customerSum   .IsEqual(GetChangeInfo<Customer>()));
         // IsTrue(articleInfo    .IsEqual(GetChangeInfo<Article>()));
            IsTrue(producerSum   .IsEqual(GetChangeInfo<Producer>()));
            IsTrue(employeeSum   .IsEqual(GetChangeInfo<Employee>()));
        }
    }
    
    static class PocUtils
    {
        public static void AddChanges<T> (this ChangeInfo<T> sum, EntityChanges<T> changes) where T: Entity {
            sum.creates += changes.creates.Count;
            sum.updates += changes.updates.Count;
            sum.deletes += changes.deletes.Count;
            // sum.patches += changes.patches.Count; // todo
        }
    }
}