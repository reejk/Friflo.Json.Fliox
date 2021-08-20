﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Graph
{
    
    public abstract  class FindTask<T> : SyncTask where T : class {
        internal                    TaskState   findState;
        
        public   abstract override  string      Details { get; }
        internal abstract override  TaskState   State   { get; }

        internal abstract void SetFindResult(Dictionary<string, T> values, Dictionary<string, EntityValue> entities);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class Find<T> : FindTask<T> where T : class
    {
        private  readonly   string      id;
        private             T           result;

        public              T           Result      => IsOk("Find.Result", out Exception e) ? result : throw e;

        internal override   TaskState   State       => findState;

        public   override   string      Details     => $"Find<{typeof(T).Name}> (id: '{id}')";
        

        internal Find(string id) {
            this.id     = id;
        }
        
        internal override void SetFindResult(Dictionary<string, T> values, Dictionary<string, EntityValue> entities) {
            TaskErrorInfo error = new TaskErrorInfo();
            var entityError = entities[id].Error;
            if (entityError == null) {
                findState.Synced = true;
                result = values[id];
                return;
            }
            error.AddEntityError(entityError);
            findState.SetError(error);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class FindRange<T> : FindTask<T> where T : class
    {
        private  readonly   HashSet<string>         ids;
        private  readonly   Dictionary<string, T>   results = new Dictionary<string, T>();

        public              T                       this[string id]      => IsOk("FindRange[]", out Exception e) ? results[id] : throw e;
        public              Dictionary<string, T>   Results { get {
            if (IsOk("FindRange.Results", out Exception e))
                return results;
            throw e;
        } }

        internal override   TaskState       State       => findState;
        public   override   string          Details     => $"FindRange<{typeof(T).Name}> (#ids: {ids.Count})";
        

        internal FindRange(ICollection<string> ids) {
            this.ids    = ids.ToHashSet();
        }
        
        internal override void SetFindResult(Dictionary<string, T> values, Dictionary<string, EntityValue> entities) {
            TaskErrorInfo error = new TaskErrorInfo();
            foreach (var id in ids) {
                var entityError = entities[id].Error;
                if (entityError == null) {
                    results.Add(id, values[id]);    
                } else {
                    error.AddEntityError(entityError);
                }
            }
            if (error.HasErrors) {
                findState.SetError(error);
                return;
            }
            findState.Synced = true;
        }
    } 
    
    
    // ----------------------------------------- ReadTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ReadTask<TKey, T> : SyncTask, IReadRefsTask<TKey, T> where T : class
    {
        internal            TaskState               state;
        internal readonly   EntitySet<TKey, T>      set;
        internal            RefsTask                refsTask;
        internal readonly   Dictionary<string, T>   results     = new Dictionary<string, T>();
        internal readonly   List<FindTask<T>>       findTasks   = new List<FindTask<T>>();

        public              Dictionary<string, T>   Results          => IsOk("ReadTask.Results", out Exception e) ? results     : throw e;
        public              T                       this[string id]  => IsOk("ReadTask[]",       out Exception e) ? results[id] : throw e;

        internal override   TaskState               State       => state;
        public   override   string                  Details     => $"ReadTask<{typeof(T).Name}> (#ids: {results.Count})";
        

        internal ReadTask(EntitySet<TKey, T> set) {
            refsTask    = new RefsTask(this);
            this.set    = set;
        }

        public Find<T> Find(string id) {
            if (id == null)
                throw new ArgumentException($"ReadTask.Find() id must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
            results.Add(id, null);
            var find = new Find<T>(id);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }
        
        public FindRange<T> FindRange(ICollection<string> ids) {
            if (ids == null)
                throw new ArgumentException($"ReadTask.FindRange() ids must not be null. EntitySet: {set.name}");
            if (State.IsSynced())
                throw AlreadySyncedError();
            results.EnsureCapacity(results.Count + ids.Count);
            foreach (var id in ids) {
                if (id == null)
                    throw new ArgumentException($"ReadTask.FindRange() id must not be null. EntitySet: {set.name}");
                results.TryAdd(id, null);
            }
            var find = new FindRange<T>(ids);
            findTasks.Add(find);
            set.intern.store.AddTask(find);
            return find;
        }

        // lab - ReadRefs by Entity Type
        public ReadRefsTask<TKey, TRef> ReadRefsOfType<TRef>() where TRef : class {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public ReadRefsTask<TKey, object> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
        
        // --- Ref
        public ReadRefTask<TKey2, TRef> ReadRef<TKey2, TRef>(Expression<Func<T, Ref<TKey2, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return ReadRefByPath<TKey2, TRef>(path);
        }
        
        public ReadRefTask<TKey2, TRef> ReadRefPath<TKey2, TRef>(RefPath<TKey2, T, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return ReadRefByPath<TKey2, TRef>(selector.path);
        }
        
        private ReadRefTask<TKey2, TRef> ReadRefByPath<TKey2, TRef>(string path) where TRef : class {
            if (refsTask.subRefs.TryGetTask(path, out ReadRefsTask subRefsTask))
                return (ReadRefTask<TKey2, TRef>)subRefsTask;
            var newQueryRefs = new ReadRefTask<TKey2, TRef>(this, path, typeof(TRef).Name, set.intern.store);
            refsTask.subRefs.AddTask(path, newQueryRefs);
            set.intern.store.AddTask(newQueryRefs);
            return newQueryRefs;
        }
        
        // --- Refs
        public ReadRefsTask<TKey, TRef> ReadRefsPath<TRef>(RefsPath<TKey, T, TRef> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TKey, TRef>(selector.path, set.intern.store);
        }

        public ReadRefsTask<TKey, TRef> ReadRefs<TRef>(Expression<Func<T, Ref<TKey, TRef>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TKey, TRef>(selector, set.intern.store);
        }
        
        public ReadRefsTask<TKey, TRef> ReadArrayRefs<TRef>(Expression<Func<T, IEnumerable<Ref<TKey, TRef>>>> selector) where TRef : class {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TKey, TRef>(selector, set.intern.store);
        }
        
        internal override void AddFailedTask(List<SyncTask> failed) {
            /*foreach (var findTask in findTasks) {
                if (findTask.State.Error.HasErrors) {
                    failed.Add(findTask);
                }
            }*/
        }
    }
}
