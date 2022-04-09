// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.GraphQL;

namespace Friflo.Json.Fliox.Hub.GraphQL
{
    internal static class Utils
    {
        internal static JsonValue CreateSchemaResponse(Pool pool, GqlSchema gqlSchema) {
            using (var pooled = pool.ObjectMapper.Get()) {
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