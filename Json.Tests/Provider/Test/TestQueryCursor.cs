
using System.Threading.Tasks;
using NUnit.Framework;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Provider.Env;

namespace Friflo.Json.Tests.Provider.Test
{
    public static class TestQueryCursor
    {
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Limit(string db) {
            var client  = await GetClient(db);
            var query   = client.testQuantify.QueryAll();
            query.limit = 2;
            await client.SyncTasks();
            AreEqual(2, query.Result.Count);
        }
        
        // Using maxCount less than available entities. So multiple query are required to return all entities.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_MultiStep(string db) {
            var client      = await GetClient(db);
            var query       = client.testQuantify.QueryAll();
            int count       = 0;
            int iterations  = 0;
            while (true) {
                query.maxCount  = 2;
                iterations++;
                await client.SyncTasks();
                
                count          += query.Result.Count;
                var cursor      = query.ResultCursor;
                if (cursor == null)
                    break;
                query           = client.testQuantify.QueryAll();
                query.cursor    = cursor;
            }
            AreEqual(3, iterations);
            AreEqual(5, count);
        }
        
        // Using maxCount greater than available entities. So a single query return all entities.
        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestQuery_Cursor_SingleStep(string db) {
            var client      = await GetClient(db);
            var query       = client.testQuantify.QueryAll();
            query.maxCount  = 100;
            await client.SyncTasks();
                
            AreEqual(5, query.Result.Count);
            IsNull(query.ResultCursor);
        }
    }
}
