﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || MYSQL

using System;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using MySqlConnector;
using static Friflo.Json.Fliox.Hub.MySQL.MySQLUtils;
using static Friflo.Json.Fliox.Hub.Host.SQL.SQLName;

// ReSharper disable UseAwaitUsing
// ReSharper disable UseIndexFromEndExpression
namespace Friflo.Json.Fliox.Hub.MySQL
{
    public sealed class MySQLContainer : EntityContainer, ISQLTable
    {
        private  readonly   TableInfo       tableInfo;
        private  readonly   ContainerInit   init;
        public   override   bool            Pretty      { get; }
        private  readonly   MySQLProvider   provider;
        
        internal MySQLContainer(string name, MySQLDatabase database, bool pretty)
            : base(name, database)
        {
            init        = new ContainerInit(database);
            tableInfo   = new TableInfo (database, name);
            Pretty      = pretty;
            provider    = database.Provider;
        }
        
        public async Task<TaskExecuteError> InitTable(ISyncConnection connection) {
            if (init.CreateTable) {
                // [MySQL :: MySQL 8.0 Reference Manual :: 11.7 Data Type Storage Requirements] https://dev.mysql.com/doc/refman/8.0/en/storage-requirements.html
                var sql = $"CREATE TABLE if not exists {name} ({ID} VARCHAR(255) PRIMARY KEY, {DATA} JSON);";
                var result = await Execute((SyncConnection)connection, sql).ConfigureAwait(false);
                if (result.Failed) {
                    return result.error;
                }
                init.tableCreated = true;
            }
            if (init.AddVirtualColumns) {
                await AddVirtualColumns(connection).ConfigureAwait(false);
                init.virtualColumnsAdded = true;
            }
            return null;
        }
        
        public async Task AddVirtualColumns(ISyncConnection syncConnection) {
            var connection = (SyncConnection)syncConnection;
            using var cmd   = Command($"SELECT * FROM {name} LIMIT 0", (SyncConnection)connection);
            var columnNames = await SQLUtils.GetColumnNames(cmd).ConfigureAwait(false);
            foreach (var column in tableInfo.columns.Values) {
                if (column == tableInfo.keyColumn || columnNames.Contains(column.name)) {
                    continue;
                }
                await AddVirtualColumn(connection, name, column, provider).ConfigureAwait(false);
            }
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new CreateEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new CreateEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new CreateEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {name} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash);
            try {
                using var cmd = Command(sql.ToString(), connection);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            } catch (Exception e) {
                return new CreateEntitiesResult { Error = DatabaseError(e.Message) };    
            }
            return new CreateEntitiesResult();
        }
        
        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new UpsertEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (command.entities.Count == 0) {
                return new UpsertEntitiesResult();
            }
            var sql = new StringBuilder();
            sql.Append($"REPLACE INTO {name} ({ID},{DATA}) VALUES\n");
            SQLUtils.AppendValuesSQL(sql, command.entities, SQLEscape.BackSlash);

            using var cmd = Command(sql.ToString(), connection);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            return new UpsertEntitiesResult();
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new ReadEntitiesResult { Error = error };
            }
            var sql = new StringBuilder();
            sql.Append($"SELECT {ID}, {DATA} FROM {name} WHERE {ID} in\n");
            SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.BackSlash);
            using var cmd = Command(sql.ToString(), connection);
            return await SQLUtils.ReadEntitiesAsync(cmd, command).ConfigureAwait(false);
        }

        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter  = command.GetFilter();
            var where   = filter.IsTrue ? "TRUE" : filter.MySQLFilter(provider);
            var sql     = SQLUtils.QueryEntitiesSQL(command, name, where);
            try {
                using var cmd = Command(sql, connection);
                return await SQLUtils.QueryEntities(cmd, command, sql).ConfigureAwait(false);
            }
            catch (MySqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(e.Message), sql = sql };
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new AggregateEntitiesResult { Error = syncConnection.Error };
            }
            if (command.type == AggregateType.count) {
                var filter  = command.GetFilter();
                var where   = filter.IsTrue ? "" : $" WHERE {filter.MySQLFilter(provider)}";
                var sql     = $"SELECT COUNT(*) from {name}{where}";

                var result  = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new AggregateEntitiesResult { Error = result.error }; }
                return new AggregateEntitiesResult { value = (long)result.value };
            }
            return new AggregateEntitiesResult { Error = NotImplemented($"type: {command.type}") };
        }

        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var syncConnection = await syncContext.GetConnectionAsync().ConfigureAwait(false);
            if (syncConnection is not SyncConnection connection) {
                return new DeleteEntitiesResult { Error = syncConnection.Error };
            }
            var error = await InitTable(connection).ConfigureAwait(false);
            if (error != null) {
                return new DeleteEntitiesResult { Error = error };
            }
            if (command.all == true) {
                var sql = $"DELETE from {name}";
                var result = await Execute(connection, sql).ConfigureAwait(false);
                if (result.Failed) { return new DeleteEntitiesResult { Error = result.error }; }
                return new DeleteEntitiesResult();    
            } else {
                var sql = new StringBuilder();
                sql.Append($"DELETE FROM  {name} WHERE {ID} in\n");
                
                SQLUtils.AppendKeysSQL(sql, command.ids, SQLEscape.BackSlash);
                using var cmd = Command(sql.ToString(), connection);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return new DeleteEntitiesResult();
            }
        }
        
        private static TaskExecuteError DatabaseError(string message) {
            return new TaskExecuteError(TaskErrorType.DatabaseError, message);
        }
    }
}

#endif