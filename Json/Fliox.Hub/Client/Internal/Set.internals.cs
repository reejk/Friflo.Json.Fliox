﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform;

// EntitySet & EntitySetBase<T> are not intended as a public API.
// These classes are declared here to simplify navigation to EntitySet<TKey, T>.
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // --------------------------------------- EntitySetBase<T> ---------------------------------------
    internal abstract partial class Set<T> : Set where T : class
    {
        internal  InstanceBuffer<CreateTask<T>>     createBuffer;
        internal  InstanceBuffer<UpsertTask<T>>     upsertBuffer;
        internal  InstanceBuffer<UpsertEntities>    upsertEntitiesBuffer;
        
        internal  abstract  Peer<T>         GetPeerById     (in JsonKey id);
        internal  abstract  Peer<T>         CreatePeer      (T entity);
        internal  abstract  JsonKey         GetEntityId     (T entity);
        
        protected Set(string name, int index, FlioxClient client) : base(name, index, client) { }
        
        internal static void ValidateKeyType(Type keyType) {
            var entityId        = EntityKey.GetEntityKey<T>();
            var entityKeyType   = entityId.GetKeyType();
            // TAG_NULL_REF
            var underlyingKeyType   = Nullable.GetUnderlyingType(keyType);
            if (underlyingKeyType != null) {
                keyType = underlyingKeyType;
            }
            if (keyType == entityKeyType)
                return;
            var type            = typeof(T);
            var entityKeyName   = entityId.GetKeyName();
            var name            = type.Name;
            var keyName         = keyType.Name;
            var error = $"key Type mismatch. {entityKeyType.Name} ({name}.{entityKeyName}) != {keyName} (EntitySet<{keyName},{name}>)";
            throw new InvalidTypeException(error);
        }
    }

    // ---------------------------------- EntitySet<TKey, T> internals ----------------------------------
    internal partial class Set<TKey, T>
    {
        private TypeMapper<T>  GetTypeMapper() => intern.typeMapper   ??= (TypeMapper<T>)client._readonly.typeStore.GetTypeMapper(typeof(T));

        
        private SetInfo GetSetInfo() {
            var info    = new SetInfo (name) { peers = peerMap.Count };
            var tasks   = GetTasks();
            SetTaskInfo(ref info, tasks);
            return info;
        }
        
        internal SyncTask[] GetTasks() {
            var allTasks    = client._intern.syncStore.tasks.GetReadOnlySpan();
            var count       = 0;
            foreach (var task in allTasks) {
                if (task.taskSet == this) {
                    count++;
                }
            }
            if (count == 0) {
                return Array.Empty<SyncTask>();    
            }
            var tasks = new SyncTask[count];
            var n = 0;
            foreach (var task in allTasks) {
                if (task.taskSet == this) {
                    tasks[n++] = task;
                }
            }
            return tasks;
        }
        
        internal DetectPatchesTask<TKey,T> DetectPatches() {
            var task    = new DetectPatchesTask<TKey,T>(this);
            AddDetectPatches(task);
            using (var pooled = client.ObjectMapper.Get()) {
                foreach (var peerPair in peerMap) {
                    TKey    key  = peerPair.Key;
                    Peer<T> peer = peerPair.Value;
                    DetectPeerPatches(key, peer, task, pooled.instance);
                }
            }
            return task;
        }
        
        internal override void Reset() {
            peerMap.Clear();
            intern.writePretty  = ClientStatic.DefaultWritePretty;
            intern.writeNull    = ClientStatic.DefaultWriteNull;
        }

        // --- internal generic entity utility methods - there public counterparts are at EntityUtils<TKey,T>
        private  static     void    SetEntityId (T entity, in JsonKey id)   => EntityKeyTMap.SetId(entity, id);
        internal override   JsonKey GetEntityId (T entity)                  => EntityKeyTMap.GetId(entity);
        internal static     TKey    GetEntityKey(T entity)                  => EntityKeyTMap.GetKey(entity);

        internal override void DetectSetPatchesInternal(DetectAllPatches allPatches, ObjectMapper mapper) {
            var task    = new DetectPatchesTask<TKey,T>(this);
            var peers   = peerMap;
            foreach (var peerPair in peers) {
                TKey    key     = peerPair.Key;
                Peer<T> peer    = peerPair.Value;
                DetectPeerPatches(key, peer, task, mapper);
            }
            if (task.Patches.Count > 0) {
                allPatches.entitySetPatches.Add(task);
                AddDetectPatches(task);
                client.AddTask(task);
            }
        }
        
        internal override Peer<T> CreatePeer (T entity) {
            var key   = GetEntityKey(entity);
            var peers = peerMap;
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                peer.SetEntity(entity);
                return peer;
            }
            var id  = KeyConvert.KeyToId(key);
            peer    = new Peer<T>(entity, id);
            peers.Add(key, peer);
            return peer;
        }
        
        private void DeletePeer (in JsonKey id) {
            var key = KeyConvert.IdToKey(id);
            peerMap.Remove(key);
        }
        
        [Conditional("DEBUG")]
        private static void AssertId(TKey key, in JsonKey id) {
            var expect = KeyConvert.KeyToId(key);
            if (!id.IsEqual(expect))
                throw new InvalidOperationException($"assigned invalid id: {id}, expect: {expect}");
        }
        
        internal bool TryGetPeerByKey(TKey key, out Peer<T> value) {
            return peerMap.TryGetValue(key, out value);
        }
        
        private Peer<T> GetOrCreatePeerByKey(TKey key, JsonKey id) {
            if (peerMap.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            if (id.IsNull()) {
                id = KeyConvert.KeyToId(key);
            } else {
                AssertId(key, id);
            }
            peer = new Peer<T>(id);
            peerMap.Add(key, peer);
            return peer;
        }

        /// use <see cref="GetOrCreatePeerByKey"/> if possible
        internal override Peer<T> GetPeerById(in JsonKey id) {
            var key = KeyConvert.IdToKey(id);
            var peers = peerMap;
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }
        
        // ReSharper disable once UnusedMember.Local
        private bool TryGetPeerByEntity(T entity, out Peer<T> value) {
            var key     = EntityKeyTMap.GetKey(entity); 
            return peerMap.TryGetValue(key, out value);
        }
        
        // --- EntitySet
        internal void SyncPeerEntities(
            List<JsonValue>         values,
            List<JsonKey>           keys,
            ObjectMapper            mapper,
            List<ApplyInfo<TKey,T>> applyInfos)
        {
            if (values.Count != keys.Count) throw new InvalidOperationException("expect values.Count == keys.Count");
            var reader      = mapper.reader;
            var count       = values.Count;
            var typeMapper  = GetTypeMapper();
            for (int n = 0; n < count; n++) {
                var id          = keys[n];
                var peer        = GetPeerById(id);

                peer.error  = null;
                var entity  = peer.NullableEntity;
                ApplyInfoType applyType;
                if (entity == null) {
                    applyType   = ApplyInfoType.EntityCreated;
                    entity      = (T)typeMapper.NewInstance();
                    SetEntityId(entity, id);
                    peer.SetEntity(entity);
                } else {
                    applyType   = ApplyInfoType.EntityUpdated;
                }
                var value = values[n];
                reader.ReadToMapper(typeMapper, value, entity, false);
                if (reader.Success) {
                    peer.SetPatchSource(value);
                } else {
                    applyType |= ApplyInfoType.ParseError;
                }
                var key = KeyConvert.IdToKey(id);
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, key, entity, value));
            }
        }
        
        private T AddEntity (in EntityValue value, Peer<T> peer, ObjectReader reader, out EntityError entityError) {
            var error = value.Error;
            if (error != null) {
                entityError     = error;
                return null;
            }
            var json = value.Json;
            if (json.IsNull()) {
                peer.SetEntity(null);   // Could delete peer instead
                peer.SetPatchSourceNull();
                entityError = null;
                return null;
            }
            var typeMapper  = GetTypeMapper();
            var entity      = peer.NullableEntity;
            if (entity == null) {
                entity          = (T)typeMapper.NewInstance();
                SetEntityId(entity, peer.id);
                peer.SetEntity(entity);
            }
            reader.ReadToMapper(typeMapper, json, entity, false);
            if (reader.Success) {
                peer.SetPatchSource(json);
                entityError = null;
                return entity;
            }
            entityError = new EntityError(EntityErrorType.ParseError, nameShort, peer.id, reader.Error.msg.ToString());
            return null;
        }
        
        // ---------------------------- get EntityValue[] from results ----------------------------
        /// <summary>Counterpart of <see cref="RemoteHostUtils.ResponseToJson"/></summary>
        private EntityValue[] GetReadResultValues (ReadEntitiesResult result) {
            if (!client._readonly.isRemoteHub) {
                return result.entities.Values;
            }
            return JsonToEntities(result.set, result.notFound, result.errors);
        }
        
        private EntityValue[] GetQueryResultValues (QueryEntitiesResult result) {
            if (!client._readonly.isRemoteHub) {
                return result.entities.Values;
            }
            return JsonToEntities(result.set, null, result.errors);
        }
        
        private EntityValue[] GetReferencesResultValues (ReferencesResult result) {
            if (!client._readonly.isRemoteHub) {
                return result.entities.Values;
            }
            return JsonToEntities(result.set, null, result.errors);
        }
        
        internal override EntityValue[] AddReferencedEntities (ReferencesResult referenceResult, ObjectReader reader)
        {
            var values  = GetReferencesResultValues(referenceResult);
            
            for (int n = 0; n < values.Length; n++) {
                var value   = values[n];
                var id      = KeyConvert.IdToKey(value.key);
                var peer    = GetOrCreatePeerByKey(id, value.key);
                AddEntity(value, peer, reader, out var error);
                if (error != null) {
                    peer.error = error;
                }
            }
            return values;
        }
        
        // ------------------------------------------------------------------------------------------
        internal void DeletePeerEntities (List<Delete<TKey>> deletes, List<ApplyInfo<TKey,T>> applyInfos) {
            var peers = peerMap;
            foreach (var delete in deletes) {
                var found   = peers.Remove(delete.key);
                var type    = found ? ApplyInfoType.EntityDeleted : default;
                applyInfos.Add(new ApplyInfo<TKey,T>(type, delete.key, default, default));
            }
        }
        
        internal void PatchPeerEntities (List<Patch<TKey>> patches, ObjectMapper mapper, List<ApplyInfo<TKey,T>> applyInfos) {
            var reader      = mapper.reader;
            var typeMapper  = GetTypeMapper();
            foreach (var patch in patches) {
                var applyType   = ApplyInfoType.EntityPatched;
                var peer        = GetOrCreatePeerByKey(patch.key, default);
                var entity      = peer.Entity;
                reader.ReadToMapper(typeMapper, patch.patch, entity, false);
                if (reader.Error.ErrSet) {
                    applyType |= ApplyInfoType.ParseError;
                }
                applyInfos.Add(new ApplyInfo<TKey,T>(applyType, patch.key, entity, patch.patch));
            }
        }

        internal override SyncTask SubscribeChangesInternal(Change change) {
            var all = Operation.FilterTrue;
            var task = SubscribeChangesFilter(change, all);
            client.AddTask(task);
            return task;
        }
        
        internal override SubscribeChanges GetSubscription() {
            return intern.subscription;
        }
        
        internal override string GetKeyName() {
            return EntityKeyTMap.GetKeyName();
        }
        
        internal override bool IsIntKey() {
            return EntityKeyTMap.IsIntKey();
        }
        
        internal override  void GetRawEntities(List<object> result) {
            foreach (var pair in Local) {
                result.Add(pair.Value);
            }
        }
    }
}