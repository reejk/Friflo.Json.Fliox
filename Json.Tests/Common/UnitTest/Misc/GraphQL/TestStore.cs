﻿using System.Threading.Tasks;
using Friflo.Json.Mapper;
using Friflo.Json.Mapper.ER;
using Friflo.Json.Mapper.ER.Database;
using Friflo.Json.Tests.Common.Utils;
using Friflo.Json.Tests.Unity.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc.GraphQL
{
    public class TestStore : LeakTestsFixture
    {
        [Test]
        public async Task WriteReadMemory() {
            var database = new MemoryDatabase();
            await WriteRead(database);
        }

        [Test]
        public async Task WriteReadFile() {
            var database = new FileDatabase(CommonUtils.GetBasePath() + "assets/db");
            await WriteRead(database);
        }

        private async Task WriteRead(EntityDatabase database) {
            using (var store = await TestRelationPoC.CreateStore(database)) {
                
                // --- cache empty
                var order = store.orders.Read("order-1");
                await store.Sync();

                WriteRead(order.Result, store);
                await AssertStore(order.Result, store);
            }
        }
        
        private static void WriteRead(Order order, EntityStore store) {
            var m = store.jsonMapper;
            m.Pretty = true;
            
            AssertWriteRead(m, order);
            AssertWriteRead(m, order.customer);
            AssertWriteRead(m, order.items[0]);
            AssertWriteRead(m, order.items[1]);
            AssertWriteRead(m, order.items[0].article);
            AssertWriteRead(m, order.items[1].article);
        }

        private static async Task AssertStore(Order order, PocStore store) {
            var order1 =    store.orders.Read("order-1");
            // await store.Sync();
            
            var article1 =  store.articles.Read("article-1");
            var article2 =  store.articles.Read("article-2");
            var customer1 = store.customers.Read("customer-1");

            await store.Sync();
            
            AreEqual(1, store.customers.Count);
            AreEqual(2, store.articles.Count);
            AreEqual(1, store.orders.Count);

            IsTrue(order1.Result      == order);
            IsTrue(customer1.Result   == order.customer.Entity);
            IsTrue(article1.Result    == order.items[0].article.Entity);
            IsTrue(article2.Result    == order.items[1].article.Entity);
        }

        private static void AssertWriteRead<T>(JsonMapper m, T entity) {
            var json    = m.Write(entity);
            var result  = m.Read<T>(json);
            AssertUtils.Equivalent(entity, result);
            // IsTrue(entity.Equals(result)); // references are equal
        }
    }
}