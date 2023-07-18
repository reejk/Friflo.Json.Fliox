
#if !UNITY_5_3_OR_NEWER

using System.Threading.Tasks;
using Friflo.Json.Tests.Provider.Client;
using NUnit.Framework;
using SqlKata;
using static Friflo.Json.Tests.Provider.Env;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Friflo.Json.Tests.Provider.Test
{
    // ReSharper disable once InconsistentNaming
    public static class Test_6_Statement
    {
        private static bool SupportSQL (string db) => /* IsSQLite(db) || */  IsMySQL(db) || IsMariaDB(db) || IsPostgres(db) || IsSQLServer(db);

        [TestCase(memory_db, Category = memory_db)] [TestCase(test_db, Category = test_db)] [TestCase(sqlite_db, Category = sqlite_db)]
        public static async Task TestStatement_Select(string db) {
            if (!SupportSQL(db)) return;
            
            var compiler    = GetCompiler();
            var query       = new Query(nameof(TestClient.testReadTypes));
            var result      = compiler.Compile(query);
            
            var client      = await GetClient(db);
            // var sql2        = "DROP TABLE IF EXISTS `testops`;";
            SqlResult ddd;
            var sqlResult   = client.std.ExecuteSQL(result.Sql);
            await client.SyncTasks();
        }
    }
}

#endif
