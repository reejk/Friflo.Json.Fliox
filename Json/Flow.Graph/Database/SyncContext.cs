﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Database.Remote;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;
using Friflo.Json.Flow.Utils;

namespace Friflo.Json.Flow.Database
{
    // ------------------------------------ SyncContext ------------------------------------
    /// <summary>
    /// One <see cref="SyncContext"/> is created per <see cref="SyncRequest"/> instance to enable
    /// multi threaded and concurrent request handling.
    /// <br></br>
    /// Note: In case of adding transaction support in future transaction data/state will be stored here.
    /// </summary>
    public class SyncContext
    {
        public  readonly        IPools  pools;
        
        public SyncContext (IPools pools) {
            this.pools = pools;
        }
    }
    
    public class ContextPools {
        internal readonly           IPools  pools;
        
        public   static readonly    Pools   SharedPools = new Pools(null);

        public ContextPools () {
            pools = new Pools(SharedPools);
        }
    }
    
    public interface IPools
    {
        ObjectPool<JsonPatcher>     JsonPatcher     { get; }
        ObjectPool<ScalarSelector>  ScalarSelector  { get; }
        ObjectPool<JsonEvaluator>   JsonEvaluator   { get; }
        ObjectPool<ObjectMapper>    ObjectMapper    { get; }
        
        void                        AssertNoLeaks ();
    }
    
    public class Pools : IPools, IDisposable 
    {
        public ObjectPool<JsonPatcher>      JsonPatcher     { get; }
        public ObjectPool<ScalarSelector>   ScalarSelector  { get; }
        public ObjectPool<JsonEvaluator>    JsonEvaluator   { get; }
        public ObjectPool<ObjectMapper>     ObjectMapper    { get; }
        
        
        // constructor present for code navigation
        internal Pools(Pools sharedPools) {
            if (sharedPools != null) {
                JsonPatcher      = new LocalPool<JsonPatcher>    (sharedPools.JsonPatcher,      "JsonPatcher");
                ScalarSelector   = new LocalPool<ScalarSelector> (sharedPools.ScalarSelector,   "ScalarSelector");
                JsonEvaluator    = new LocalPool<JsonEvaluator>  (sharedPools.JsonEvaluator,    "JsonEvaluator");
                ObjectMapper     = new LocalPool<ObjectMapper>   (sharedPools.ObjectMapper,     "ObjectMapper");
            } else {
                JsonPatcher      = new SharedPool<JsonPatcher>   (() => new JsonPatcher());
                ScalarSelector   = new SharedPool<ScalarSelector>(() => new ScalarSelector());
                JsonEvaluator    = new SharedPool<JsonEvaluator> (() => new JsonEvaluator());
                ObjectMapper     = new SharedPool<ObjectMapper>  (() => new ObjectMapper(SyncTypeStore.Get()));
            }
        }

        public void Dispose() {
            JsonPatcher.Dispose();
            ScalarSelector.Dispose();
            JsonEvaluator.Dispose();
            ObjectMapper.Dispose();
        }
        
        public void AssertNoLeaks() {
            JsonPatcher.    AssertNoLeaks();
            ScalarSelector. AssertNoLeaks();
            JsonEvaluator.  AssertNoLeaks();
            ObjectMapper.   AssertNoLeaks();
        }
    }
}