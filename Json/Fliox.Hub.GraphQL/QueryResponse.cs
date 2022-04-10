// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class QueryResponse
    {
        internal static QueryResult ProcessSyncResponse(
            RequestContext  context,
            List<Query>     queries,
            SyncResponse    syncResponse)
        {
            var data        = new Dictionary<string, JsonValue>(queries.Count);
            var taskResults = syncResponse.tasks;
            using (var pooled = context.ObjectMapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                for (int n = 0; n < queries.Count; n++) {
                    var query       = queries[n];
                    var taskResult  = taskResults[n];
                    var queryResult = ProcessTaskResult(query, taskResult, writer);
                    data.Add(query.name, queryResult);
                }
                var response            = new GqlResponse { data = data };
                var jsonResponse        = new JsonValue(writer.WriteAsArray(response));

                context.Write(jsonResponse, 0, "application/json", 200);
            }
            return default;
        }
        
        private static JsonValue ProcessTaskResult(in Query query, SyncTaskResult result, ObjectWriter writer) {
            if (result is TaskErrorResult taskError) {
                return new JsonValue(writer.WriteAsArray(taskError));
            }
            switch (query.type) {
                case QueryType.Query:       return QueryEntitiesResult  (query, result, writer);
                case QueryType.ReadById:    return ReadEntitiesResult   (query, result, writer);
            }
            throw new InvalidOperationException($"unexpected query type: {query.type}");
        }
        
        private static JsonValue QueryEntitiesResult(Query query, SyncTaskResult result, ObjectWriter writer) {
            return new JsonValue("{}");
        }
        
        private static JsonValue ReadEntitiesResult(Query query, SyncTaskResult result, ObjectWriter writer) {
            return new JsonValue("{}");
        }
    }
}

#endif
