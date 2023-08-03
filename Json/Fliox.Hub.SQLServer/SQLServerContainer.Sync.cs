﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER || SQLSERVER

using System.Collections.Generic;
using System.Data;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using System.Data.SqlClient;
using static Friflo.Json.Fliox.Hub.SQLServer.SQLServerUtils;


namespace Friflo.Json.Fliox.Hub.SQLServer
{
    internal sealed partial class SQLServerContainer
    {

        SqlCommand      readCommand;
        SqlParameter    sqlParam;
        
        /// <summary>sync version of <see cref="ReadEntitiesAsync"/></summary>
        public override ReadEntitiesResult ReadEntities(ReadEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new ReadEntitiesResult { Error = syncConnection.Error };
            }
            try {
                if (tableType == TableType.Relational) {
                    if (false) {
                        var sql = SQL.ReadRelational(this, command);
                        using var reader = connection.ExecuteReaderSync(sql);
                        return SQLTable.ReadEntitiesSync(reader, command, tableInfo, syncContext);
                    } else {
                        using var pooled = syncContext.SQL2Object.Get();
                        var sql2Object = pooled.instance;
                        if (readCommand == null) {
                            var sql = sql2Object.sb;
                            sql.Clear();
                            sql.Append("SELECT "); SQLTable.AppendColumnNames(sql, tableInfo);
                            sql.Append($" FROM {name} WHERE {tableInfo.keyColumn.name} in (@ids);\n");
                            readCommand = new SqlCommand(sql.ToString(), connection.sqlInstance);
                            sqlParam = readCommand.Parameters.Add("@ids", SqlDbType.NVarChar, 100);
                            readCommand.Prepare();
                        }
                        sql2Object.sb.Clear();
                        sqlParam.Value = SQLUtils.AppendKeysSQL2(sql2Object.sb, command.ids, SQLEscape.PrefixN).ToString();
                        using var reader = connection.ExecuteReaderSync(readCommand);
                        return SQLTable.ReadObjects(reader, command, sql2Object);
                    }
                } else {
                    var sql = SQL.ReadJsonColumn(this,command);
                    using var reader = connection.ExecuteReaderSync(sql);
                    return SQLUtils.ReadJsonColumnSync(reader, command);
                }
            }
            catch (SqlException e) {
                return new ReadEntitiesResult { Error = new TaskExecuteError(GetErrMsg(e)) };
            }
        }
        
        /// <summary>sync version of <see cref="QueryEntitiesAsync"/></summary>
        public override QueryEntitiesResult QueryEntities(QueryEntities command, SyncContext syncContext) {
            var syncConnection = syncContext.GetConnectionSync();
            if (syncConnection is not SyncConnection connection) {
                return new QueryEntitiesResult { Error = syncConnection.Error };
            }
            var sql = SQL.Query(this, command);
            try {
                List<EntityValue> entities;
                using var reader = connection.ExecuteReaderSync(sql);
                if (tableType == TableType.Relational) {
                    entities = SQLTable.QueryEntitiesSync(reader, tableInfo, syncContext);
                } else {
                    entities = SQLUtils.QueryJsonColumnSync(reader);
                }
                return SQLUtils.CreateQueryEntitiesResult(entities, command, sql);
            }
            catch (SqlException e) {
                return new QueryEntitiesResult { Error = new TaskExecuteError(GetErrMsg(e)), sql = sql };
            }
        }
    }
}

#endif