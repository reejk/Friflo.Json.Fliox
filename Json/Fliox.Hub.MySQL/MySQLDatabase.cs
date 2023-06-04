// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLUtils;

namespace Friflo.Json.Fliox.Hub.MySQL
{
    public class MySQLDatabase : EntityDatabase, ISQLDatabase
    {
        public              bool            Pretty                  { get; init; } = false;
        public              bool            AutoCreateDatabase      { get; init; } = true;
        public              bool            AutoCreateTables        { get; init; } = true;
        public              bool            AutoAddVirtualColumns   { get; init; } = true;
        
        internal readonly   string          connectionString;
        
        public   override   string          StorageType => "MySQL";
        internal virtual    MySQLProvider   Provider    => MySQLProvider.MY_SQL;
        
        public MySQLDatabase(string dbName, string connectionString, DatabaseSchema schema = null, DatabaseService service = null)
            : base(dbName, schema, service)
        {
            this.connectionString = connectionString;
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MySQLContainer(name.AsString(), this, Pretty);
        }
        
        public override async Task<SyncConnection> GetConnectionAsync()  {
            Exception openException;
            try {
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);                
            } catch (Exception e) {
                openException = e;
            }
            if (!AutoCreateDatabase) {
                return new SyncConnection(openException);
            }
            try {
                await CreateDatabaseIfNotExistsAsync(connectionString).ConfigureAwait(false);
            } catch (Exception e) {
                return new SyncConnection(e);
            }
            try {
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync().ConfigureAwait(false);
                return new SyncConnection(connection);
            } catch (Exception e) {
                return new SyncConnection(e);
            }
        }
    }
    
    public sealed class MariaDBDatabase : MySQLDatabase
    {
        public    override   string          StorageType => "MariaDB";
        internal  override   MySQLProvider   Provider    => MySQLProvider.MARIA_DB;
        
        public MariaDBDatabase(string dbName, string connectionString, DatabaseSchema schema = null, DatabaseService service = null)
            : base(dbName, connectionString, schema, service)
        { }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new MySQLContainer(name.AsString(), this, Pretty);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    // This is needed to enable following features in .NET framework and .NET core <= 3.1 projects:
    // - init only setter properties. See [Init only setters - C# 9.0 draft specifications | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/init
    // - record types
    internal static class IsExternalInit { }
}

#endif
