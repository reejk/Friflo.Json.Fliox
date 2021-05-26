﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Graph.Internal.Map;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Graph
{
    internal struct StoreIntern
    {
        internal readonly   TypeStore                       typeStore;
        internal readonly   TypeCache                       typeCache;
        internal readonly   ObjectMapper                    jsonMapper;
        // private  readonly   JsonReadError                errorHandler;

        internal readonly   ObjectPatcher                   objectPatcher;
        
        internal readonly   EntityDatabase                  database;
        internal readonly   Dictionary<Type,   EntitySet>   setByType;
        internal readonly   Dictionary<string, EntitySet>   setByName;
        internal readonly   ContextPools                    contextPools;
        
        // --- non readonly
        internal            SyncStore                       sync;
        internal            LogTask                         tracerLogTask;

        
        internal StoreIntern(TypeStore typeStore, EntityDatabase database, ObjectMapper jsonMapper) {
            this.typeStore      = typeStore;
            this.database       = database;
            this.jsonMapper     = jsonMapper;
            this.typeCache      = jsonMapper.writer.TypeCache;
            setByType           = new Dictionary<Type, EntitySet>();
            setByName           = new Dictionary<string, EntitySet>();
            objectPatcher       = new ObjectPatcher(jsonMapper);
            sync                = new SyncStore();
            contextPools        = new  ContextPools();
            tracerLogTask       = null;
        }
    }

    // --------------------------------------- EntityStore ---------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class EntityStore : ITracerContext, IDisposable
    {
        // Keep all EntityStore fields in StoreIntern to enhance debugging overview.
        // Reason: EntityStore is extended by application and add multiple EntitySet fields.
        //         So internal fields are encapsulated in field intern.
        // ReSharper disable once InconsistentNaming
        internal            StoreIntern     _intern;
        public              TypeStore       TypeStore => _intern.typeStore;

        public              StoreInfo       StoreInfo  => new StoreInfo(_intern.setByType); 
        public   override   string          ToString() => StoreInfo.ToString();


        protected EntityStore(EntityDatabase database) {
            var typeStore = new TypeStore();
            typeStore.typeResolver.AddGenericTypeMapper(RefMatcher.Instance);
            typeStore.typeResolver.AddGenericTypeMapper(EntityMatcher.Instance);
            var errorHandler = new JsonReadError();
            var jsonMapper = new ObjectMapper(typeStore, errorHandler) {
                TracerContext = this
            };
            _intern = new StoreIntern(typeStore, database, jsonMapper);
        }
        
        public void Dispose() {
            _intern.contextPools.Dispose();
            _intern.objectPatcher.Dispose();
            _intern.jsonMapper.Dispose();
            _intern.typeStore.Dispose();
        }

        // --------------------------------------- public interface --------------------------------------- 
        public async Task Sync() {
            SyncRequest syncRequest = CreateSyncRequest();
            var syncContext = new SyncContext(_intern.contextPools.pools);
            SyncResponse response = await ExecuteSync(syncRequest, syncContext);
            var result = HandleSyncResponse(syncRequest, response);

            var errorCount = result.failed.Count;
            if (errorCount > 0)
                throw new SyncResultException(result.failed);
            syncContext.pools.AssertNoLeaks();
        }
        
        public async Task<SyncResult> TrySync() {
            SyncRequest syncRequest = CreateSyncRequest();
            var syncContext = new SyncContext(_intern.contextPools.pools);
            SyncResponse response = await ExecuteSync(syncRequest, syncContext);
            var result = HandleSyncResponse(syncRequest, response);
            syncContext.pools.AssertNoLeaks();
            return result;
        }
        
        /// <see cref="SyncWait"/> is redundant -> made private. Keep it for exploring (Unity)
        private void SyncWait() {
            SyncRequest syncRequest = CreateSyncRequest();
            var syncContext = new SyncContext(_intern.contextPools.pools);
            var responseTask = ExecuteSync(syncRequest, syncContext);
            // responseTask.Wait();  
            SyncResponse response = responseTask.Result;  // <--- synchronous Sync point!!
            HandleSyncResponse(syncRequest, response);
            syncContext.pools.AssertNoLeaks();
        }

        public LogTask LogChanges() {
            var task = _intern.sync.CreateLog();
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.LogSetChangesInternal(task);
            }
            AddTask(task);
            return task;
        }
        
        // ------------------------------------------- internals -------------------------------------------
        private async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            SyncResponse response;
            try {
                response = await _intern.database.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            }
            catch (Exception e) {
                response = new SyncResponse{error = $"{e.GetType().Name}: {e.Message}"};
            }
            return response;
        }
        
        internal void AddTask(SyncTask task) {
            _intern.sync.appTasks.Add(task);
        }
        
        internal EntitySet<T> EntitySet<T>() where T : Entity
        {
            Type entityType = typeof(T);
            if (_intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<T>)set;
            
            set = new EntitySet<T>(this);
            return (EntitySet<T>)set;
        }

        private SyncRequest CreateSyncRequest() {
            var syncRequest = new SyncRequest { tasks = new List<DatabaseTask>() };
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                var setInfo = set.SetInfo;
                var curTaskCount = syncRequest.tasks.Count;
                set.Sync.AddTasks(syncRequest.tasks);
                AssertTaskCount(setInfo, syncRequest.tasks.Count - curTaskCount);
            }
            return syncRequest;
        }

        [Conditional("DEBUG")]
        private static void AssertTaskCount(SetInfo setInfo, int taskCount) {
            int expect  = setInfo.tasks; 
            if (expect != taskCount)
                throw new InvalidOperationException($"Unexpected task.Count. expect: {expect}, got: {taskCount}");
        }

        private void SetErrors(SyncResponse response) {
            var createErrors = response.createErrors;
            if (createErrors != null) {
                foreach (var createError in createErrors) {
                    createError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[createError.Key];
                    set.Sync.createErrors = createError.Value.errors;
                }
            }
            var updateErrors = response.updateErrors;
            if (updateErrors != null) {
                foreach (var updateError in updateErrors) {
                    updateError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[updateError.Key];
                    set.Sync.updateErrors = updateError.Value.errors;
                }
            }
            var patchErrors = response.patchErrors;
            if (patchErrors != null) {
                foreach (var patchError in patchErrors) {
                    patchError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[patchError.Key];
                    set.Sync.patchErrors = patchError.Value.errors;
                }
            }
            var deleteErrors = response.deleteErrors;
            if (deleteErrors != null) {
                foreach (var deleteError in deleteErrors) {
                    deleteError.Value.SetInferredErrorFields();
                    var set = _intern.setByName[deleteError.Key];
                    set.Sync.deleteErrors = deleteError.Value.errors;
                }
            }
        }

        private SyncResult HandleSyncResponse(SyncRequest syncRequest, SyncResponse response) {
            SyncResult syncResult;
            try {
                TaskErrorResult                         syncError;
                Dictionary<string, ContainerEntities>   containerResults;
                if (response.error == null) {
                    response.AssertResponse(syncRequest);
                    syncError = null;
                    containerResults = response.results;
                    foreach (var containerResult in containerResults) {
                        var set = _intern.setByName[containerResult.Key];
                        set.SyncContainerEntities(containerResult.Value);
                    }
                    SetErrors(response);
                } else {
                    syncError = new TaskErrorResult {
                        message = response.error,
                        type    = TaskErrorResultType.SyncError
                    };
                    containerResults = new Dictionary<string, ContainerEntities>();
                }

                var tasks = syncRequest.tasks;
                var results = response.tasks;
                for (int n = 0; n < tasks.Count; n++) {
                    var task = tasks[n];
                    TaskType    taskType = task.TaskType;
                    TaskResult  result;
                    if (syncError == null) {
                        result = results[n];
                        var actual = result.TaskType;
                        if (actual != TaskType.Error) {
                            if (taskType != actual) {
                                var msg = $"Expect task type of response matches request. index:{n} expect: {taskType} actual: {actual}";
                                throw new InvalidOperationException(msg);
                            }
                        }
                    } else {
                        result = syncError;
                    }

                    switch (taskType) {
                        case TaskType.Create:
                            var create = (CreateEntities) task;
                            EntitySet set = _intern.setByName[create.container];
                            set.Sync.CreateEntitiesResult(create, result);
                            break;
                        case TaskType.Update:
                            var update = (UpdateEntities) task;
                            set = _intern.setByName[update.container];
                            set.Sync.UpdateEntitiesResult(update, result);
                            break;
                        case TaskType.Read:
                            var readList = (ReadEntitiesList) task;
                            set = _intern.setByName[readList.container];
                            containerResults.TryGetValue(readList.container, out ContainerEntities entities);
                            set.Sync.ReadEntitiesListResult(readList, result, entities);
                            break;
                        case TaskType.Query:
                            var query = (QueryEntities) task;
                            set = _intern.setByName[query.container];
                            containerResults.TryGetValue(query.container, out ContainerEntities queryEntities);
                            set.Sync.QueryEntitiesResult(query, result, queryEntities);
                            break;
                        case TaskType.Patch:
                            var patch = (PatchEntities) task;
                            set = _intern.setByName[patch.container];
                            set.Sync.PatchEntitiesResult(patch, result);
                            break;
                        case TaskType.Delete:
                            var delete = (DeleteEntities) task;
                            set = _intern.setByName[delete.container];
                            set.Sync.DeleteEntitiesResult(delete, result);
                            break;
                    }
                }
                _intern.sync.LogResults();
            }
            finally {
                var failed = new List<SyncTask>();
                foreach (SyncTask task in _intern.sync.appTasks) {
                    task.AddFailedTask(failed);
                }
                syncResult = new SyncResult(_intern.sync.appTasks, failed, response.error);
                // new EntitySet task are collected (scheduled) in a new EntitySetSync instance and requested via next Sync() 
                foreach (var setPair in _intern.setByType) {
                    EntitySet set = setPair.Value;
                    set.ResetSync();
                }
                _intern.sync = new SyncStore();
            }
            return syncResult;
        }
    }
    
    public static class StoreExtension
    {
        public static EntityStore Store(this ITracerContext store) {
            return (EntityStore)store;
        }
    }
    
    internal class JsonReadError : IErrorHandler
    {
        /// throw no exceptions on errors. Errors are handled by checking <see cref="ObjectReader.Success"/> 
        public void HandleError(int pos, ref Bytes message) {
        }
    }
}
