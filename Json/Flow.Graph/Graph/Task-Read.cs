﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Burst;  // UnityExtension.TryAdd()
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class Find<T> : SyncTask where T : Entity
    {
        private  readonly   string      id;
        internal readonly   ReadTask<T> task; 

        public              T           Result      => Synced ? task.idMap[id] : throw RequiresSyncError($"Find.Result requires Sync().");

        internal override   bool        Synced      => task.Synced;
        internal override   string      Label       => $"ReadId<{typeof(T).Name}> id: {id}";
        public   override   string      ToString()  => Label;

        internal Find(ReadTask<T> task, string id) {
            this.id     = id;
            this.task   = task;
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FindRange<T> : SyncTask where T : Entity
    {
        private  readonly   HashSet<string>         ids;
        private  readonly   ReadTask<T>             task; 

        public              T                       this[string id]      => Synced ? task.idMap[id] : throw RequiresSyncError($"FindRange[] requires Sync().");
        public              Dictionary<string, T>   Results { get {
            if (Synced) {
                var result = new Dictionary<string, T>(ids.Count);
                foreach (var id in ids) {
                    result.Add(id, task.idMap[id]);
                }
                return result;
            }
            throw RequiresSyncError($"ReadIds.Results requires Sync()."); }
        }

        internal override   bool        Synced      => task.Synced;
        internal override   string      Label       => $"ReadIds<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string      ToString()  => Label;

        internal FindRange(ReadTask<T> task, ICollection<string> ids) {
            this.ids    = ids.ToHashSet();
            this.task   = task;
        }
    } 
    
    
    // ----------------------------------------- ReadTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadTask<T> : SyncTask, IReadRefsTask<T> where T : Entity
    {
        internal            bool                    synced;
        internal readonly   EntitySet<T>            set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<string, T>   idMap = new Dictionary<string, T>();
        
        public              Dictionary<string, T>   Results          => Synced ? idMap      : throw RequiresSyncError("ReadTask.Results requires Sync().");
        public              T                       this[string id]  => Synced ? idMap[id]  : throw RequiresSyncError("ReadTask[] requires Sync().");

        internal override   bool                    Synced      => synced;
        internal override   string                  Label       => $"ReadTask<{typeof(T).Name}> #ids: {idMap.Count}";
        public   override   string                  ToString()  => Label;

        internal ReadTask(EntitySet<T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public Find<T> Find(string id) {
            if (id == null)
                throw new InvalidOperationException($"ReadTask.ReadId() id must not be null. EntitySet: {set.name}");
            if (Synced)
                throw AlreadySyncedError();
            idMap.Add(id, null);
            return new Find<T>(this, id);
        }
        
        public FindRange<T> FindRange(ICollection<string> ids) {
            if (ids == null)
                throw new InvalidOperationException($"ReadTask.ReadIds() ids must not be null. EntitySet: {set.name}");
            if (Synced)
                throw AlreadySyncedError();
#if !UNITY_5_3_OR_NEWER
            idMap.EnsureCapacity(idMap.Count + ids.Count);
#endif
            foreach (var id in ids) {
                if (id == null)
                    throw new NullReferenceException($"ReadTask.ReadIds() id must not be null. EntitySet: {set.name}");
                idMap.TryAdd(id, null);
            }
            return new FindRange<T>(this, ids);
        }

        // lab - ReadRefs by Entity Type
        public ReadRefsTask<TValue> ReadRefsOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefsTask<Entity> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
        
        // --- Ref
        public ReadRefTask<TValue> ReadRef<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            string path = MemberSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TValue>(path);
        }
        
        public ReadRefTask<TValue> ReadRefByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            if (refsTask.subRefs.TryGetTask(selector, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TValue>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TValue>(this, selector, typeof(TValue).Name);
            refsTask.subRefs.AddTask(selector, newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TValue> ReadRefs<TValue>(Expression<Func<T, Ref<TValue>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadArrayRefs<TValue>(Expression<Func<T, IEnumerable<Ref<TValue>>>> selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TValue>(selector);
        }
        
        public ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (Synced)
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TValue>(selector);
        }
    }
}
