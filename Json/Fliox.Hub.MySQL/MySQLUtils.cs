// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Schema.Definition;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
namespace Friflo.Json.Fliox.Hub.MySQL
{
    internal static class MySQLUtils
    {
        internal static async Task<SQLResult> Execute(SyncConnection connection, string sql) {
            try {
                using var reader = await connection.ExecuteReaderAsync(sql).ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false)) {
                    var value = reader.GetValue(0);
                    return new SQLResult(value); 
                    // Console.WriteLine($"MySQL version: {value}");
                }
                return default;
            }
            catch (MySqlException e) {
                return new SQLResult(e.Message);
            }
        }
        
        internal static async Task AddVirtualColumn(SyncConnection connection, string table, ColumnInfo column, MySQLProvider provider) {
            var type = ConvertContext.GetSqlType(column, provider);
            var colName = column.name; 
            switch (provider) {
                case MySQLProvider.MARIA_DB: {
var sql =
$@"ALTER TABLE {table}
ADD COLUMN IF NOT EXISTS `{colName}` {type}
GENERATED ALWAYS AS (JSON_VALUE({DATA}, '$.{colName}')) VIRTUAL;";
                    await Execute(connection, sql).ConfigureAwait(false);
                    return;
                }
                case MySQLProvider.MY_SQL: {
                    var asStr  = GetColumnType(column);
var sql = $@"ALTER TABLE {table}
ADD COLUMN `{colName}` {type}
GENERATED ALWAYS AS {asStr} VIRTUAL;";
                    await Execute(connection, sql).ConfigureAwait(false);
                    return;
                }
            }
        }
        
        private static string GetColumnType(ColumnInfo column) {
            var colName = column.name;
            var asStr   = $"(JSON_VALUE({DATA}, '$.{colName}'))";
            switch (column.typeId) {
                // case StandardTypeId.DateTime:
                //    return $"(CONVERT({asStr}, DATETIME(3)))";
                case StandardTypeId.Boolean:
                    return $"(case when {asStr} = 'true' then 1 when {asStr} = 'false' then 0 end)";
                default:
                    return asStr;
            }
        }
        
        internal static async Task CreateDatabaseIfNotExistsAsync(string connectionString) {
            var dbmsConnectionString = GetDbmsConnectionString(connectionString, out var database);
            using var connection = new MySqlConnection(dbmsConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            
            var sql = $"CREATE DATABASE IF NOT EXISTS {database}";
            using var cmd = new MySqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        
        private static string GetDbmsConnectionString(string connectionString, out string database) {
            var builder  = new MySqlConnectionStringBuilder(connectionString);
            database = builder.Database;
            builder.Remove("Database");
            return builder.ToString();
        }
    }
}

#endif