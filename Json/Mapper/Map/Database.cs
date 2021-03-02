﻿using System;
using System.Collections.Generic;

namespace Friflo.Json.Mapper.Map
{
    public class Database
    {
        private readonly Dictionary<Type, DatabaseContainer> containers = new Dictionary<Type, DatabaseContainer>();

        protected void AddContainer(DatabaseContainer container) {
            Type entityType = container.EntityType;
            containers.Add(entityType, container);
        }

        public DatabaseContainer GetContainer(Type entityType) {
            return containers[entityType];
        }
    }
    
    public abstract class DatabaseContainer
    {
        public abstract  Type       EntityType  { get; }
        public abstract  int        Count       { get; }
        
        protected internal abstract void     AddEntity   (Entity entity);
        protected internal abstract void     RemoveEntity(string id);
        protected internal abstract Entity   GetEntity   (string id);
    }

    public abstract class DatabaseContainer<T> : DatabaseContainer where T : Entity
    {
        public override Type             EntityType => typeof(T);
        
        // ---
        public abstract void    Add(T entity);
        public abstract T       this[string id] { get; set; } // Item[] Property
    }
    
    public class MemoryContainer<T> : DatabaseContainer<T> where T : Entity
    {
        private readonly Dictionary<string, T> map = new Dictionary<string, T>();

        public override int Count => map.Count;

        protected internal override void AddEntity   (Entity entity) {
            T typedEntity = (T) entity;
            map.Add(typedEntity.id, typedEntity);
        }

        protected internal override void RemoveEntity(string id) {
            map.Remove(id);
        }

        protected internal override Entity GetEntity(string id) {
            return map[id];
        }
        
        // ---
        public override void Add(T entity) {
            map.Add(entity.id, entity);
        }
        
        public override T this[string id] {
            get => map[id];
            set => map[id] = value;
        }
    }
}