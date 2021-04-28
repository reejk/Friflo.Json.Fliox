﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal interface ISetTask
    {
        string  Label { get; }
    }
    
    internal struct RefsTask
    {
        private readonly    ISetTask    task;
        internal            bool        synced;
        internal            SubRefs     subRefs;
        

        internal RefsTask(ISetTask task) {
            this.task       = task;
            this.subRefs    = new SubRefs();
            this.synced     = false;
        }
        
        internal Exception AlreadySyncedError() {
            return new TaskAlreadySyncedException($"Task already synced. {task.Label}");
        }
        
        internal Exception RequiresSyncError(string message) {
            return new TaskNotSyncedException($"{message} {task.Label}");
        }
        
        internal ReadRefsTask<TValue> ReadRefsByExpression<TValue>(Expression expression) where TValue : Entity {
            string path = MemberSelector.PathFromExpression(expression, out _);
            return ReadRefsByPath<TValue>(path);
        }
        
        internal ReadRefsTask<TValue> ReadRefsByPath<TValue>(string selector) where TValue : Entity {
            if (subRefs.TryGetTask(selector, out ReadRefsTask subRefsTask))
                return (ReadRefsTask<TValue>)subRefsTask;
            var newQueryRefs = new ReadRefsTask<TValue>(task, selector, typeof(TValue).Name);
            subRefs.AddTask(selector, newQueryRefs);
            return newQueryRefs;
        }
    }
    
    public struct SubRefs // : IEnumerable <BinaryPair>   <- not implemented to avoid boxing
    {
        /// key: <see cref="ReadRefsTask.Selector"/>
        private     Dictionary<string, ReadRefsTask>    map; // map == null if no tasks added
        
        public    int                                 Count => map?.Count ?? 0;
        public    ReadRefsTask                        this[string key] => map[key];
        
        public bool TryGetTask(string selector, out ReadRefsTask subRefsTask) {
            if (map == null) {
                subRefsTask = null;
                return false;
            }
            return map.TryGetValue(selector, out subRefsTask);
        }
        
        public void AddTask(string selector, ReadRefsTask subRefsTask) {
            if (map == null) {
                map = new Dictionary<string, ReadRefsTask>();
            }
            map.Add(selector, subRefsTask);
        }

        // return ValueIterator instead of IEnumerator<ReadRefsTask> to avoid boxing 
        public ValueIterator<string, ReadRefsTask> GetEnumerator() {
            return new ValueIterator<string, ReadRefsTask>(map);
        }
    }
}