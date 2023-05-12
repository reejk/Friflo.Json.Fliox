// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLITE

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using SQLitePCL;

namespace Friflo.Json.Fliox.Hub.SQLite
{
    public sealed class SQLiteDatabase : EntityDatabase
    {
        public              bool        Pretty      { get; init; } = false;
        public              int?        Throughput  { get; init; } = null;
        
        internal readonly   sqlite3                                 sqliteDB;
        private             Dictionary<string, SQLitePrimaryKey>    keys;
        
        public   override   string      StorageType => "SQLite " + SQLiteUtils.GetVersion(sqliteDB);
        
        /// <summary>
        /// Open or create a database with the given <paramref name="path"/>.<br/>
        /// Create an Im-Memory <paramref name="path"/> is <c>":memory:"</c><br/>
        /// See: <a href="https://www.sqlite.org/inmemorydb.html">SQLite - In-Memory Databases</a>
        /// </summary>
        /// <returns></returns>
        public SQLiteDatabase(string dbName, string path)
            : base(dbName)
        {
            var rc = raw.sqlite3_open(path, out sqliteDB);
            if (rc != raw.SQLITE_OK) throw new InvalidOperationException($"sqlite3_open failed. error: {rc}");
        }
        
        public override EntityContainer CreateContainer(in ShortString name, EntityDatabase database) {
            return new SQLiteContainer(name.AsString(), this, Pretty);
        }
        
        static SQLiteDatabase() {
            raw.SetProvider(new SQLite3Provider_e_sqlite3());
        }
        
        internal void CreateSchema() {
            if (keys != null) {
                return;
            }
            var schema = Schema;
            keys = new Dictionary<string, SQLitePrimaryKey>();
            var fields = schema.typeSchema.RootType.Fields;
            foreach (var container in schema.GetContainers()) {
                var field           = fields.First(f => f.name == container);
                var keyField        = field.type.KeyField;
                var containerFields = field.type.Fields;
                var key             = containerFields.First(f => f.name == keyField);
                bool isString       = key.type == schema.typeSchema.StandardTypes.String;
            }
        }
    }
    
    internal sealed class SQLitePrimaryKey {
        
    }
    
    internal sealed class SQLiteQueryEnumerator : QueryEnumerator
    {
        internal readonly   sqlite3_stmt    stmt;
        internal readonly   string          sql;
        
        public   override   JsonKey         Current     => throw new NotImplementedException("not applicable");
        public   override   bool            MoveNext()  => throw new NotImplementedException("not applicable");
        
        internal SQLiteQueryEnumerator(sqlite3_stmt stmt, string sql) {
            this.stmt   = stmt;
            this.sql    = sql;
        }
        
        protected override void DisposeEnumerator() {
            raw.sqlite3_finalize(stmt);
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
