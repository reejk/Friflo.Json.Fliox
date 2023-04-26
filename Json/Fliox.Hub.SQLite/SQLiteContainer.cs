﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteContainer : EntityContainer
    {
        private  readonly   SQLiteDatabase      sqliteDB;
        private             bool                tableExists;
        public   override   bool                Pretty      { get; }
        
        internal SQLiteContainer(string name, SQLiteDatabase database, bool pretty)
            : base(name, database)
        {
            sqliteDB    = database;
            Pretty      = pretty;
        }

        private bool EnsureContainerExists(out TaskExecuteError error) {
            if (tableExists) {
                error = null;
                return true;
            }
            var sql = $"CREATE TABLE IF NOT EXISTS {name} (id TEXT PRIMARY KEY, data TEXT NOT NULL)";
            var success = SQLiteUtils.Execute(sqliteDB.sqliteDB, sql, out error);
            if (success) {
                tableExists = true;
            }
            return success;
        }
        
        public override Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            var result = CreateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override CreateEntitiesResult CreateEntities(CreateEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new CreateEntitiesResult { Error = error };
            }
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "BEGIN TRANSACTION", out  error)) {
                return new CreateEntitiesResult { Error = error };
            }
            var sql = $@"INSERT INTO {name} VALUES(?,?)";
            if (!SQLiteUtils.Prepare(sqliteDB.sqliteDB, sql, out var stmt, out error)) {
                return new CreateEntitiesResult { Error = error };    
            }
            SQLiteUtils.AppendValues(stmt, command.entities);
            raw.sqlite3_finalize(stmt);
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "END TRANSACTION", out error)) {
                return new CreateEntitiesResult { Error = error };
            }
            return new CreateEntitiesResult();
        }
        
        public override Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            var result = UpsertEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override UpsertEntitiesResult UpsertEntities(UpsertEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "BEGIN TRANSACTION", out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            var sql = $@"INSERT INTO {name} VALUES(?,?) ON CONFLICT(id) DO UPDATE SET data=excluded.data";
            if (!SQLiteUtils.Prepare(sqliteDB.sqliteDB, sql, out var stmt, out error)) {
                return new UpsertEntitiesResult { Error = error };    
            }
            SQLiteUtils.AppendValues(stmt, command.entities);
            raw.sqlite3_finalize(stmt);
            
            if (!SQLiteUtils.Exec(sqliteDB.sqliteDB, "END TRANSACTION", out error)) {
                return new UpsertEntitiesResult { Error = error };
            }
            return new UpsertEntitiesResult();
        }
        
        public override Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            var result = ReadEntities(command, syncContext);
            return Task.FromResult(result);
        }

        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new ReadEntitiesResult { Error = error };
            }
            var sb = new StringBuilder();
            SQLiteUtils.AppendIds(sb, command.ids);
            var ids = sb.ToString();
            var sql = $"SELECT id, data FROM {name} WHERE id in ({ids})";
            if (!SQLiteUtils.Prepare(sqliteDB.sqliteDB, sql, out var stmt, out error)) {
                return new ReadEntitiesResult { Error = error };    
            }
            var values = new List<EntityValue>();
            SQLiteUtils.ReadValues(stmt, values, syncContext.MemoryBuffer);
            return new ReadEntitiesResult { entities = values.ToArray() };
        }
        
        public override Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            var result = QueryEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            var filter = command.GetFilter().SQLiteFilter();
            var sql = $"SELECT id, data FROM {name} WHERE {filter}";
            if (!SQLiteUtils.Prepare(sqliteDB.sqliteDB, sql, out var stmt, out error)) {
                return new QueryEntitiesResult { Error = error };    
            }
            var values = new List<EntityValue>();
            SQLiteUtils.ReadValues(stmt, values, syncContext.MemoryBuffer);
            return new QueryEntitiesResult { entities = values.ToArray() }; 
        }
        
        public override Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var result = AggregateEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        private AggregateEntitiesResult AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new AggregateEntitiesResult { Error = error };
            }
            throw new NotImplementedException();
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            var result = DeleteEntities(command, syncContext);
            return Task.FromResult(result);
        }
        
        public override DeleteEntitiesResult DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            if (!EnsureContainerExists(out var error)) {
                return new DeleteEntitiesResult { Error = error };
            }
            throw new NotImplementedException();
        }
    }
}

#endif