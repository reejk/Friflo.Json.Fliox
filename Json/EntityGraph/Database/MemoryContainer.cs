﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Mapper.Map.Val;

namespace Friflo.Json.EntityGraph.Database
{
    public class MemoryDatabase : EntityDatabase
    {
        private readonly    bool    pretty;

        public MemoryDatabase(bool pretty = false) {
            this.pretty = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, pretty);
        }
    }
    
    public class MemoryContainer : EntityContainer
    {
        private readonly    Dictionary<string, string>  payloads    = new Dictionary<string, string>();
        
        public  override    bool                        Pretty { get; }

        public MemoryContainer(string name, EntityDatabase database, bool pretty) : base(name, database) {
            Pretty = pretty;
        }


        public override void CreateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                payloads[entity.key] = entity.value.json;
            }
        }

        public override void UpdateEntities(ICollection<KeyValue> entities) {
            foreach (var entity in entities) {
                if (!payloads.TryGetValue(entity.key, out string _))
                    throw new InvalidOperationException($"Expect Entity with id {entity.key} in DatabaseContainer: {name}");
                payloads[entity.key] = entity.value.json;
            }
        }

        public override ICollection<KeyValue> ReadEntities(ICollection<string> ids) {
            var result = new List<KeyValue>();
            foreach (var id in ids) {
                payloads.TryGetValue(id, out var payload);
                var entry = new KeyValue {
                    key     = id,
                    value   = new JsonValue{ json = payload }
                };
                result.Add(entry);
            }
            return result;
        }
    }
}