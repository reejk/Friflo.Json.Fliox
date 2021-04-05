﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.EntityGraph
{
    // =======================================   CRUD   ==========================================
    
    // ----------------------------------------- Read<> -----------------------------------------
    public class Read<T> where T : Entity
    {
        private  readonly   string          id;
        internal            T               result;
        internal            bool            synced;
        private  readonly   EntitySet<T>    set;

        internal Read(string id, EntitySet<T> set) {
            this.id = id;
            this.set = set;
        }
            
        public T Result {
            get {
                if (synced)
                    return result;
                throw new InvalidOperationException($"Read().Result requires Sync(). Entity: {typeof(T).Name} id: {id}");
            }
        }
        
        // lab - prototype API
        public Dependency<TValue> DependencyPath<TValue>(string selector) where TValue : Entity {
            var readDeps = set.GetReadDeps<TValue>(selector);
            Dependency<TValue> newDependency = new Dependency<TValue>(id);
            readDeps.dependencies.Add(id, newDependency);
            return newDependency;
        }
        
        public Dependencies<TValue> DependenciesPath<TValue>(string selector) where TValue : Entity {
            var readDeps = set.GetReadDeps<TValue>(selector);
            Dependencies<TValue> newDependency = new Dependencies<TValue>(id);
            readDeps.dependencies.Add(id, newDependency);
            return newDependency;
        }

        // lab - expression API
        public Dependency<TValue> Dependency<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity 
        {
            return default;
        }
        
        // lab - expression API
        public IEnumerable<Dependency<TValue>> Dependencies<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity 
        {
            return default;
        }

        // lab - dependencies by Entity Type
        public IEnumerable<Dependency<TValue>> DependenciesOfType<TValue>() where TValue : Entity
        {
            return default;
        }
        
        // lab - all dependencies
        public IEnumerable<Dependency<Entity>> AllDependencies()
        {
            return default;
        }
    }
    
    
    // ----------------------------------------- Create<> -----------------------------------------
    public class Create<T> where T : Entity
    {
        private readonly    T           entity;
        private readonly    EntityStore store;

        internal            T           Entity => entity;
        
        internal Create(T entity, EntityStore entityStore) {
            this.entity = entity;
            this.store = entityStore;
        }

        // public T Result  => entity;
    }

    
    
    // ----------------------------------------- Dependency<> -----------------------------------------
    public class Dependency
    {
        internal readonly   string      parentId;
        internal readonly   bool        singleEntity;

        internal Dependency(string parentId, bool singleEntity) {
            this.parentId       = parentId;
            this.singleEntity   = singleEntity;
        }
    }
    
    public class Dependency<T> : Dependency where T : Entity
    {
        internal            string      id;
        internal            T           entity;

        public              string      Id      => id       ?? throw new InvalidOperationException("Dependency not synced"); 
        public              T           Entity  => entity   ?? throw new InvalidOperationException("Dependency not synced"); 

        internal Dependency(string parentId) : base (parentId, true) { }
    }
    
    public class Dependencies<T> : Dependency where T : Entity
    {
        public readonly     List<Dependency<T>>     dependencies = new List<Dependency<T>>();

        internal Dependencies(string parentId) : base (parentId, false) { }
    }
    
    internal class ReadDeps
    {
        internal readonly   string                          selector;
        internal readonly   Type                            entityType;
        internal readonly   Dictionary<string, Dependency>  dependencies = new Dictionary<string, Dependency>();
        
        internal ReadDeps(string selector, Type entityType) {
            this.selector = selector;
            this.entityType = entityType;
        }
    }
}

