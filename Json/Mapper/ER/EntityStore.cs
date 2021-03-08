﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Mapper.ER.Database;
using Friflo.Json.Mapper.ER.Map;
using Friflo.Json.Mapper.Map;

namespace Friflo.Json.Mapper.ER
{
    public class EntityStore : IDisposable
    {
        internal readonly   EntityDatabase  database;
        public  readonly    TypeStore       typeStore = new TypeStore();
        public readonly     JsonMapper      jsonMapper;
        
        public EntityStore(EntityDatabase database) {
            this.database = database;
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            jsonMapper = new JsonMapper(typeStore) {
                EntityStore = this
            };
        }
        
        public void Dispose() {
            jsonMapper.Dispose();
            typeStore.Dispose();
        }
        
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal readonly Dictionary<Type, EntityStoreContainer> containers = new Dictionary<Type, EntityStoreContainer>();

        public async Task Sync() {
            foreach (var container in containers.Values) {
                await container.SyncContainer(database);
            }
        }

        public EntitySet<T> GetSet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityStoreContainer set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }
    }
    
    public abstract class EntityStoreContainer
    {
        protected internal abstract Task SyncContainer   (EntityDatabase database);
    }

    public class Read<T>
    {
        internal string id;
        internal T      result;

        internal Read(string id) {
            this.id = id;
        }
            
        public T Result { get => result; }
    }
    
    public class Create<T>
    {
        internal T      entity;

        internal Create(T entity) {
            this.entity = entity;
        }
            
        public T Result { get => entity; }
    }

   
    public class EntitySet<T> : EntityStoreContainer where T : Entity
    {
        private readonly    TypeMapper<T>           typeMapper;
        private readonly    JsonMapper              jsonMapper;
        private readonly    EntityContainer         container;
        private readonly    Dictionary<string, T>   map         = new Dictionary<string, T>();  // todo -> HashSet<>
        private readonly    List<Read<T>>           reads       = new List<Read<T>>();  // todo -> HashSet<>
        private readonly    List<T>                 creates     = new List<T>();  // todo -> HashSet<>

        public              int                     Count       => map.Count;
        
        public EntitySet(EntityStore store) {
            store.containers[typeof(T)] = this;
            jsonMapper = store.jsonMapper;
            typeMapper = (TypeMapper<T>)store.typeStore.GetTypeMapper(typeof(T));
            container = store.database.GetContainer(typeof(T).Name);
        }
        
        internal void CreateEntity   (T entity) {
            if (map.TryGetValue(entity.id, out T value)) {
                if (value != entity)
                    throw new InvalidOperationException("");
                return;
            }
            map.Add(entity.id, entity);
        }

        internal T GetEntity(string id) {
            if (map.TryGetValue(id, out T entity))
                return entity;
            entity = (T)typeMapper.CreateInstance();
            entity.id = id;
            map.Add(id, entity);
            return entity;
        }
        
        public Read<T> Read(string id) {
            var read = new Read<T>(id);
            reads.Add(read);
            return read;
        }
        
        public Create<T> Create(T entity) {
            var create = new Create<T>(entity);
            creates.Add(entity);
            return create;
        }

        protected internal override async Task SyncContainer(EntityDatabase database) {
            // creates
            if (creates.Count > 0) {
                List<KeyValue> entries = new List<KeyValue>();
                foreach (var entity in creates) {
                    var entry = new KeyValue {
                        key = entity.id,
                        value = jsonMapper.Write(entity)
                    };
                    entries.Add(entry);
                }
                await container.CreateEntities(entries);
                creates.Clear();
            }
            
            // reads
            if (reads.Count > 0) {
                List<string> ids = new List<string>();
                reads.ForEach(read => ids.Add(read.id));
                var entries = await container.ReadEntities(ids);
                if (entries.Count != reads.Count)
                    throw new InvalidOperationException($"Expect returning same number of entities {entries.Count} as number ids {ids.Count}");
                
                int n = 0;
                foreach (var entry in entries) {
                    var entity = GetEntity(entry.key);
                    reads[n++].result = entity;
                    jsonMapper.ReadTo(entry.value, entity);
                }
                reads.Clear();
            }
        }
        
    }
}