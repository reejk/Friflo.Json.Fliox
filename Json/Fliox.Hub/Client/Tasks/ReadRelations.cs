﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{

    // could be an interface, but than internal used methods would be public (C# 8.0 enables internal interface methods) 
    public abstract class ReadRelationsTask : SyncTask
    {
        internal            TaskState   state;
        internal abstract   string      Selector    { get; }
        internal abstract   string      Container   { get; }
        internal abstract   string      KeyName     { get; }
        internal abstract   bool        IsIntKey    { get; }
        internal abstract   SubRefs     SubRefs     { get; }
        
        internal abstract void    SetResult (EntitySet set, HashSet<JsonKey> ids);
    }

    /// ensure all tasks returning <see cref="ReadRelationsTask{T}"/>'s provide the same interface
    public interface IReadRelationsTask<T> where T : class
    {
        ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>>              selector) where TRef : class;
        ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>>             selector) where TRef : class  where TRefKey : struct;
        ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class;
        ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>>selector) where TRef : class  where TRefKey : struct;
        ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef>                       selector) where TRef : class;
    }

    // ----------------------------------------- ReadRefsTask<T> -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class ReadRelationsTask<T> : ReadRelationsTask, IReadRelationsTask<T>  where T : class
    {
        private             RefsTask    refsTask;
        private             List<T>     result;
        private   readonly  SyncTask    parent;
        private   readonly  FlioxClient store;
            
        public              List<T>     Result  => IsOk("ReadRelationsTask.Result", out Exception e) ? result      : throw e;
        
        internal  override  TaskState   State   => state;
        public    override  string      Details => $"{parent.GetLabel()} -> {Selector}";
            
        internal  override  string      Selector  { get; }
        internal  override  string      Container { get; }
        internal  override  string      KeyName   { get; }
        internal  override  bool        IsIntKey  { get; }
        
        internal  override  SubRefs     SubRefs => refsTask.subRefs;


        internal ReadRelationsTask(SyncTask parent, string selector, string container, string keyName, bool isIntKey, FlioxClient store)
        {
            refsTask        = new RefsTask(this);
            this.parent     = parent;
            this.Selector   = selector;
            this.Container  = container;
            this.KeyName    = keyName;
            this.IsIntKey   = isIntKey;
            this.store      = store;
        }

        internal override void SetResult(EntitySet set, HashSet<JsonKey> ids) {
            var entitySet = (EntitySetBase<T>) set;
            result = new List<T>(ids.Count);
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var id in ids) {
                var peer = entitySet.GetPeerById(id);
                if (peer.error == null) {
                    result.Add(peer.Entity);
                } else {
                    entityErrorInfo.AddEntityError(peer.error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            }
        }
        
        // --- IReadRefsTask<T>
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, TRefKey?>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey>>> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, Expression<Func<T, IEnumerable<TRefKey?>>> selector) where TRef : class where TRefKey : struct {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(relation, selector, store);
        }
        
        public ReadRelationsTask<TRef> ReadRelations<TRefKey, TRef>(EntitySet<TRefKey, TRef> relation, RelationsPath<TRef> selector) where TRef : class {
            if (State.IsExecuted()) throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(relation, selector.path, store);
        }
    }
}
