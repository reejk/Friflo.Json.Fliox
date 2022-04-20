// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.GraphQL;
using Friflo.Json.Fliox.Utils;

// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS0649
namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal class GqlRequest {
        public  string                      query;
        public  string                      operationName;
        public  Dictionary<string,string>   variables;
    }
        
    internal class GqlResponse {
        public  Dictionary <string, JsonValue>  data;
    }
    
    internal class GqlQueryResult {
        public  int             count;
        public  string          cursor;
        public  List<JsonValue> items;
    }
    
    internal static class ModelUtils
    {
        internal static JsonValue CreateSchemaResponse(ObjectPool<ObjectMapper> mapper, GqlSchema gqlSchema) {
            using (var pooled = mapper.Get()) {
                var writer              = pooled.instance.writer;
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                var schemaJson          =  new JsonValue(writer.WriteAsArray(gqlSchema));
                
                var data = new Dictionary<string, JsonValue> {
                    { "__schema", schemaJson }
                };
                var response = new GqlResponse { data = data };
                writer.Pretty           = true;
                writer.WriteNullMembers = false;
                return new JsonValue(writer.WriteAsArray(response));
            }
        }
    }
}