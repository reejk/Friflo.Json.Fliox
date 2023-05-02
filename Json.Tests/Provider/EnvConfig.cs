#if !UNITY_5_3_OR_NEWER

using System;
using System.Threading.Tasks;
using Friflo.Json.Tests.Common.Utils;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Npgsql;

namespace Friflo.Json.Tests.Provider
{
    public static class EnvConfig
    {
        private static IConfiguration InitConfiguration() {
            var basePath        = CommonUtils.GetBasePath();
            var appSettings     = basePath + "appsettings.test.json";
            var privateSettings = basePath + "appsettings.private.json";
            return new ConfigurationBuilder().AddJsonFile(appSettings).AddJsonFile(privateSettings).Build();
        }
        
        // --- CosmosDB
        public static CosmosClient CreateCosmosClient() {
            var config      = InitConfiguration();
            var endpointUri = config["EndPointUri"];    // The Azure Cosmos DB endpoint for running this sample.
            var primaryKey  = config["PrimaryKey"];     // The primary key for the Azure Cosmos account.
            var options     = new CosmosClientOptions { ApplicationName = "Friflo.Playground" };
            return new CosmosClient(endpointUri, primaryKey, options);
        }

        // --- MySQL / MariaDB
        public static async Task<MySqlConnection> OpenMySQLConnection(string provider) {
            var config              = InitConfiguration();
            string connectionString = config[provider];
            if (connectionString == null) {
                throw new ArgumentException($"provider not found in appsettings. provider: {provider}");
            }
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
        
        // --- PostgreSQL
        public static async Task<NpgsqlConnection> OpenPostgresConnection() {
            var config              = InitConfiguration();
            string connectionString = config["postgres"];
            var connection          = new NpgsqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
        
        // --- SQL Server
        public static async Task<SqlConnection> OpenSQLServerConnection() {
            var config              = InitConfiguration();
            string connectionString = config["sqlserver"];
            var connection          = new SqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
    }
}

#endif
