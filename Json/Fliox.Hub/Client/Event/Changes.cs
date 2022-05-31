// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void ChangeSubscriptionHandler         (EventContext context);
    public delegate void ChangeSubscriptionHandler<TKey, T>(Changes<TKey, T> changes, EventContext context) where T : class;
    
    public abstract class Changes
    {
        /// <summary> total number of container changes </summary>
        public              int                         Count       => ChangeInfo.Count;
        /// <summary> number of changes per mutation type: creates, upserts, deletes and patches </summary>
        public              ChangeInfo                  ChangeInfo  { get; } = new ChangeInfo();
        /// <summary> name of the container the changes are referring to </summary>
        public    abstract  string                      Container   { get; }
        /// <summary> raw JSON values of created container entities </summary>
        public              IReadOnlyList<JsonValue>    RawCreates  => rawCreates;
        /// <summary> raw JSON values of upserted container entities </summary>
        public              IReadOnlyList<JsonValue>    RawUpserts  => rawUpserts;
        
        [DebuggerBrowsable(Never)]  internal            bool            added;
        [DebuggerBrowsable(Never)]  internal  readonly  List<JsonValue> rawCreates  = new List<JsonValue>();
        [DebuggerBrowsable(Never)]  internal  readonly  List<JsonValue> rawUpserts  = new List<JsonValue>();

        internal  abstract  Type    GetEntityType();
        internal  abstract  void    Clear       ();
        internal  abstract  void    AddDeletes  (HashSet<JsonKey> ids);
        internal  abstract  void    AddPatches  (Dictionary<JsonKey, EntityPatch> patches);
        internal  abstract  void    ApplyChangesTo  (EntitySet entitySet);
    }
    
    /// <summary>
    /// Contain the changes (mutations) made to a container subscribed with <see cref="EntitySet{TKey,T}.SubscribeChanges"/>. <br/>
    /// Following properties provide type-safe access to the different types of container changes 
    /// <list type="bullet">
    ///   <item> <see cref="Creates"/> - the created container entities</item>
    ///   <item> <see cref="Upserts"/> - the upserted container entities</item>
    ///   <item> <see cref="Deletes"/> - the keys of removed container entities</item>
    ///   <item> <see cref="Patches"/> - the patches applied to container entities</item>
    /// </list>
    /// </summary>
    public sealed class Changes<TKey, T> : Changes where T : class
    {
        /// <summary> return the keys of removed container entities </summary>
        public              List<TKey>          Deletes { get; } = new List<TKey>();
        /// <summary> return patches applied to container entities </summary>
        public              List<Patch<TKey>>   Patches { get; } = new List<Patch<TKey>>();
        public    override  string              ToString()      => ChangeInfo.ToString();       
        public    override  string              Container       { get; }
        internal  override  Type                GetEntityType() => typeof(T);
        
        [DebuggerBrowsable(Never)] private          List<T>         creates;
        [DebuggerBrowsable(Never)] private          List<T>         upserts;
        [DebuggerBrowsable(Never)] private readonly ObjectMapper    objectMapper;

        /// <summary> called via <see cref="Event.SubscriptionProcessor.GetChanges"/> </summary>
        internal Changes(EntitySet<TKey, T> entitySet, ObjectMapper mapper) {
            Container       = entitySet.name;
            objectMapper    = mapper;
        }
        
        internal override void Clear() {
            added   = false;
            creates = null;
            upserts = null;
            Deletes.Clear();
            Patches.Clear();
            
            rawCreates.Clear();
            rawUpserts.Clear();
            //
            ChangeInfo.Clear();
        }
        
        /// <summary> return the entities created in a container </summary>
        public List<T> Creates { get {
            if (creates != null)
                return creates;
            // create entities on demand
            var entities = rawCreates;
            creates = new List<T>(entities.Count); // list could be reused
            foreach (var create in entities) {
                var entity = objectMapper.Read<T>(create);
                creates.Add(entity);
            }
            return creates;
        } }
        
        /// <summary> return the entities upserted in a container </summary>
        public List<T> Upserts { get {
            if (upserts != null)
                return upserts;
            // create entities on demand
            var entities = rawUpserts;
            upserts = new List<T>(entities.Count); // list could be reused
            foreach (var upsert in entities) {
                var entity = objectMapper.Read<T>(upsert);
                upserts.Add(entity);
            }
            return upserts;
        } }

        internal override void AddDeletes  (HashSet<JsonKey> ids) {
            foreach (var id in ids) {
                TKey    key      = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                Deletes.Add(key);
            }
            ChangeInfo.deletes += ids.Count;
        }
        
        internal override void AddPatches(Dictionary<JsonKey, EntityPatch> entityPatches) {
            foreach (var pair in entityPatches) {
                var     id          = pair.Key;
                var     entityPatch = pair.Value;
                TKey    key         = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                var     patch       = new Patch<TKey>(key, id, entityPatch.patches);
                Patches.Add(patch);
            }
            ChangeInfo.patches += entityPatches.Count;
        }
        
        internal override void ApplyChangesTo  (EntitySet entitySet) {
            var set = (EntitySet<TKey, T>)entitySet;
            ApplyChangesTo(set);
        }
        
        /// <summary> Apply the container changes to the given <paramref name="entitySet"/> </summary>
        public void ApplyChangesTo(EntitySet<TKey, T> entitySet, Change change = ChangeFlags.All) {
            if (Count == 0)
                return;
            var client = entitySet.intern.store;
            using (var pooled = client._intern.pool.ObjectMapper.Get()) {
                var mapper          = pooled.instance;
                var localCreates    = rawCreates;
                if ((change & Change.create) != 0 && localCreates.Count > 0) {
                    var entityKeys = GetKeysFromEntities (client, entitySet.GetKeyName(), localCreates);
                    SyncPeerEntities(entitySet, entityKeys, localCreates, mapper);
                }
                var localUpserts    = rawUpserts;
                if ((change & Change.upsert) != 0 && localUpserts.Count > 0) {
                    var entityKeys = GetKeysFromEntities (client, entitySet.GetKeyName(), localUpserts);
                    SyncPeerEntities(entitySet, entityKeys, localUpserts, mapper);
                }
                if ((change & Change.patch) != 0) {
                    entitySet.PatchPeerEntities(Patches, mapper);
                }
            }
            if ((change & Change.delete) != 0) {
                entitySet.DeletePeerEntities(Deletes);
            }
        }
        
        private static List<JsonKey> GetKeysFromEntities(FlioxClient client, string keyName, List<JsonValue> entities) {
            var processor = client._intern.EntityProcessor();
            var keys = new List<JsonKey>(entities.Count);
            foreach (var entity in entities) {
                if (!processor.GetEntityKey(entity, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                keys.Add(key);
            }
            return keys;
        }
        
        private static void SyncPeerEntities (EntitySet set, List<JsonKey> keys, List<JsonValue> entities, ObjectMapper mapper) {
            if (keys.Count != entities.Count)
                throw new InvalidOperationException("Expect equal counts");
            var syncEntities = new Dictionary<JsonKey, EntityValue>(entities.Count, JsonKey.Equality);
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = keys[n];
                var value = new EntityValue(entity);
                syncEntities.Add(key, value);
            }
            // todo simplify - creating a Dictionary<,> is overkill
            set.SyncPeerEntities(syncEntities, mapper);
        }
    }
    
    public readonly struct Patch<TKey> {
        public    readonly  TKey                key;
        public    readonly  List<JsonPatch>     patches;
        
        internal  readonly  JsonKey             id;

        public  override    string              ToString() => key.ToString();
        
        public Patch(TKey key, in JsonKey id, List<JsonPatch> patches) {
            this.id         = id;
            this.key        = key;
            this.patches    = patches;
        }
    }
    

    internal abstract class ChangeCallback {
        internal abstract void InvokeCallback(Changes entityChanges, EventContext context);
    }
    
    internal sealed class GenericChangeCallback<TKey, T> : ChangeCallback where T : class
    {
        private  readonly   ChangeSubscriptionHandler<TKey, T>   handler;
        
        internal GenericChangeCallback (ChangeSubscriptionHandler<TKey, T> handler) {
            this.handler = handler;
        }
        
        internal override void InvokeCallback(Changes entityChanges, EventContext context) {
            var changes = (Changes<TKey,T>)entityChanges;
            handler(changes, context);
        }
    }
}