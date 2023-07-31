// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal partial class SyncSet<TKey, T>
    {
        // --- read one entity
        private void ReadEntityResult(ReadEntities task, ReadEntitiesResult result, FindTask<TKey, T> read, ObjectMapper mapper) {
            if (result.Error != null) {
                var taskError = SyncRequestTask.TaskError(result.Error);
                SetReadOneTaskError(read, taskError);
                return;
            }
            var  entityErrorInfo = new TaskErrorInfo();
            if (task.ContainerType == ContainerType.Values) {
                AddReadEntity(ref entityErrorInfo, result, read, mapper.reader);
            } else {
                AddReadObject(ref entityErrorInfo, result, read);
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                // SetReadTaskError(read, entityErrorInfo); <- must not be called
                return;
            }
            read.state.Executed = true;
            AddReferencesResult(task.references, result.references, read.relations.subRelations, mapper.reader);
        }
        
        private static void SetReadOneTaskError(FindTask<TKey, T> read, TaskErrorResult taskError) {
            TaskErrorInfo error = new TaskErrorInfo(taskError);
            read.state.SetError(error);
            SetSubRelationsError(read.relations.subRelations, error);
        }
        
        // --- read multiple entities
        private void ReadEntitiesResult(ReadEntities task, ReadEntitiesResult result, ReadTask<TKey, T> read, ObjectMapper mapper) {
            if (result.Error != null) {
                var taskError = SyncRequestTask.TaskError(result.Error);
                SetReadTaskError(read, taskError);
                return;
            }
            var  entityErrorInfo = new TaskErrorInfo();
            if (task.ContainerType == ContainerType.Values) {
                AddReadEntities(ref entityErrorInfo, result, read, mapper.reader);
            } else {
                AddReadObjects(ref entityErrorInfo, result, read);
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                // SetReadTaskError(read, entityErrorInfo); <- must not be called
                return;
            }
            read.state.Executed = true;
            AddReferencesResult(task.references, result.references, read.relations.subRelations, mapper.reader);
        }
        
        private static void SetReadTaskError(ReadTask<TKey, T> read, TaskErrorResult taskError) {
            TaskErrorInfo error = new TaskErrorInfo(taskError);
            read.state.SetError(error);
            SetSubRelationsError(read.relations.subRelations, error);
            foreach (var findTask in read.findTasks) {
                findTask.findState.SetError(error);
            }
        }

        // ---------------------------------------- add value / values  ----------------------------------------
        /// <summary>Counterpart of <see cref="RemoteHostUtils.ResponseToJson"/></summary>
        private EntityValue[] GetReadResultValues (ReadEntitiesResult result) {
            if (!set.client._readonly.hub.IsRemoteHub) {
                return result.entities.values;
            }
            return set.JsonToEntities(result.set, result.notFound, result.errors);
        }
        
        // SYNC_READ : read entity
        private void AddReadEntity(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, FindTask<TKey, T> read, ObjectReader reader)
        {
            var values  = GetReadResultValues(result);
            var id      = KeyConvert.KeyToId(read.key);
            var peer    = set.GetOrCreatePeerByKey(read.key, id);
            if (values.Length == 0) {
                peer.SetPatchSourceNull();
                peer.SetEntity(null);
                return;
            }
            ref var value   = ref values[0];
            if (!id.IsEqual(value.key)) {
                entityErrorInfo = new TaskErrorInfo(TaskErrorType.InvalidResponse, $"expect id {value.key}");
                return;
            }
            read.result = set.AddEntity(value, peer, reader, out var error);
            if (error == null) {
                return;
            }
            entityErrorInfo.AddEntityError(error);
        }
        
        // SYNC_READ : read entities
        private void AddReadEntities(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, ReadTask<TKey, T> read, ObjectReader reader)
        {
            var values      = GetReadResultValues(result);
            var readResult  = read.result;
            
            foreach (var value in values) {
                var id = KeyConvert.IdToKey(value.key);
                if (!readResult.ContainsKey(id)) {
                    continue;
                }
                var peer = set.GetOrCreatePeerByKey(id, value.key);
                readResult[id] = set.AddEntity(value, peer, reader, out var error);
                if (error == null) {
                    continue;
                }
                entityErrorInfo.AddEntityError(error);
            }
            // set all peers to null not found in result.entities
            foreach (var pair in readResult) {
                if (pair.Value != default) {
                    continue;
                }
                if (!set.TryGetPeerByKey(pair.Key, out var peer)) {
                    // peer.SetPatchSourceNull();
                    // peer.SetEntity(null);
                }
            }
            var taskError = entityErrorInfo.TaskError;
            foreach (var findTask in read.findTasks) {
                findTask.SetFindResult(read.result, taskError);
            }
        }
        
        // ---------------------------------------- add object / objects  ----------------------------------------
        // SYNC_READ : read object
        private void AddReadObject(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, FindTask<TKey, T> read)
        {
            var objects = result.entities.objects;
            var id      = KeyConvert.KeyToId(read.key);
            var peer    = set.GetOrCreatePeerByKey(read.key, id);
            if (objects.Length == 0) {
                peer.SetEntity(null);
                return;
            }
            var entityObj   = objects[0];
            if (!id.IsEqual(entityObj.key)) {
                entityErrorInfo = new TaskErrorInfo(TaskErrorType.InvalidResponse, $"expect id {entityObj.key}");
                return;
            }
            var current = peer.NullableEntity;
            if (current == null) {
                peer.SetEntity((T)entityObj.obj);
                return;
            }
            var typeMapper  = set.GetTypeMapper();
            typeMapper.MemberwiseCopy(entityObj.obj, current);
            read.result = current;
        }
        
        // SYNC_READ : read objects
        private TaskErrorInfo AddReadObjects(ref TaskErrorInfo entityErrorInfo, ReadEntitiesResult result, ReadTask<TKey, T> read)
        {
            /* var objects = readEntities.objectMap;
            foreach (var id in task.ids.GetReadOnlySpan()) {
                if (!objects.TryGetValue(id, out object value)) {
                    // AddEntityResponseError(id, entities, ref entityErrorInfo);
                    continue;
                }
                // var json = value.Json;  // in case of RemoteClient json is "null"
                // var value = json.IsNull();
                if (value == null) {
                    // don't remove missing requested peer from EntitySet.peers to preserve info about its absence
                    continue;
                }
                var key     = KeyConvert.IdToKey(id);
                var peer    = set.GetOrCreatePeerByKey(key, id);
                read.result[key] = peer.Entity;
            }
            foreach (var findTask in read.findTasks) {
                findTask.SetFindResult(read.result);
            } */
            return default;
        }
        
        // ---------------------------------------- query values ----------------------------------------
        private void QueryEntitiesResult(
            QueryEntities       task,
            QueryEntitiesResult queryResult,
            QueryTask<TKey, T>  query,
            ObjectReader        reader)
        {
            var entityErrorInfo = new TaskErrorInfo();
            query.sql           = queryResult.sql;
            query.resultCursor  = queryResult.cursor;
            var values          = GetReadResultValues(queryResult);
            query.entities      = values;
            var results         = query.result = new List<T>(values.Length);
            foreach (var value in values)
            {
                var key     = KeyConvert.IdToKey(value.key);
                var peer    = set.GetOrCreatePeerByKey(key, value.key);
                var entity  = set.AddEntity(value, peer, reader, out var error);
                if (error == null) {
                    results.Add(entity);
                    continue;
                }
                entityErrorInfo.AddEntityError(error);
            }
            if (entityErrorInfo.HasErrors) {
                query.state.SetError(entityErrorInfo);
                SetSubRelationsError(query.relations.subRelations, entityErrorInfo);
                return;
            }
            AddReferencesResult(task.references, queryResult.references, query.relations.subRelations, reader);
            query.state.Executed = true;
        }
        
        private EntityValue[] GetReadResultValues (QueryEntitiesResult result) {
            if (!set.client._readonly.hub.IsRemoteHub) {
                return result.entities.values;
            }
            return set.JsonToEntities(result.set, null, result.errors);
        }
    }
}