﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Mapper.ER
{
    public class EntityCache
    {
        private readonly EntityDatabase database;
        
        public EntityCache(EntityDatabase database) {
            this.database = database;
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, EntityCacheContainer> containers = new Dictionary<Type, EntityCacheContainer>();

        public void Sync() {
            foreach (var container in containers.Values) {
                container.SyncContainer(database);
            }
        }

        protected void AddContainer(EntityCacheContainer cache) {
            Type entityType = cache.EntityType;
            containers.Add(entityType, cache);
        }

        public EntityCacheContainer<T> GetContainer<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (containers.TryGetValue(entityType, out EntityCacheContainer container))
                return (EntityCacheContainer<T>)container;
            
            containers[entityType] = container = new MemoryCacheContainer<T>();
            return (EntityCacheContainer<T>)container;
        }
    }
    
    public abstract class EntityCacheContainer
    {
        public abstract  Type       EntityType  { get; }
        public abstract  int        Count       { get; }
        
        protected internal abstract void     AddEntity   (Entity entity);
        protected internal abstract Entity   GetEntity   (string id);
        
        protected internal abstract void     SyncContainer   (EntityDatabase database);
    }

    public abstract class EntityCacheContainer<T> : EntityCacheContainer where T : Entity
    {
        public override Type    EntityType => typeof(T);
        
        // ---
        public abstract void    Add(T entity);
        public abstract T       this[string id] { get; } // Item[] Property
    }
    
    public class MemoryCacheContainer<T> : EntityCacheContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T>  map                 = new Dictionary<string, T>();
        private readonly HashSet<string>        unresolvedEntities  = new HashSet<string>();

        public override int Count => map.Count;

        protected internal override void AddEntity   (Entity entity) {
            T typedEntity = (T) entity;
            if (map.TryGetValue(entity.id, out T value)) {
                if (value != entity)
                    throw new InvalidOperationException("");
                return;
            }
            map.Add(typedEntity.id, typedEntity);
        }

        protected internal override Entity GetEntity(string id) {
            if (map.TryGetValue(id, out T entity))
                return entity;
            unresolvedEntities.Add(id);
            return null;
        }

        protected internal override void SyncContainer(EntityDatabase database) {
            foreach (var id in unresolvedEntities) {
                EntityContainer<T> container = database.GetContainer<T>();
                var entity = container[id];
                map.Add(id, entity);
            }
        }
        
        // ---
        public override void Add(T entity) {
            map.Add(entity.id, entity);
        }
        
        public override T this[string id] => map[id];
    }
}