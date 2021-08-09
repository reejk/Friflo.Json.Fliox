﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public delegate EntityValue WriteModifier (EntityValue value);
    public delegate EntityPatch PatchModifier (EntityPatch patch);
    
    public class WriteModifierDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        private readonly    Dictionary<string, WriteModifiers>   writeModifiers  = new Dictionary<string, WriteModifiers>();
        private readonly    Dictionary<string, PatchModifiers>   patchModifiers  = new Dictionary<string, PatchModifiers>();
        
        public WriteModifierDatabase(EntityDatabase local) {
            this.local = local;
        }
        
        public void ClearErrors() {
            foreach (var pair in writeModifiers) {
                var container = pair.Value;
                container.writes.Clear();
            }
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return local.CreateContainer(name, database);
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            foreach (var task in syncRequest.tasks) {
                switch (task) {
                    case CreateEntities createEntities:
                        if (writeModifiers.TryGetValue(createEntities.container, out var write)) {
                            write.ModifyWrites(createEntities.entities);
                        }
                        break;
                    case UpdateEntities updateEntities:
                        if (writeModifiers.TryGetValue(updateEntities.container, out write)) {
                            write.ModifyWrites(updateEntities.entities);
                        }
                        break;
                    case PatchEntities patchEntities:
                        if (patchModifiers.TryGetValue(patchEntities.container, out var patch)) {
                            patch.ModifyPatches(patchEntities.patches);
                        }
                        break;
                }
            }
            return await local.ExecuteSync(syncRequest, messageContext);
        }

        public WriteModifiers GetWriteModifiers<TEntity>() where TEntity : Entity {
            var name = typeof(TEntity).Name;
            if (!writeModifiers.TryGetValue(name, out var writeModifier)) {
                writeModifier = new WriteModifiers();
                writeModifiers.Add(name, writeModifier);
            }
            return writeModifier;
        }
        
        public PatchModifiers GetPatchModifiers<TEntity>() where TEntity : Entity {
            var name = typeof(TEntity).Name;
            if (!patchModifiers.TryGetValue(name, out var patchModifier)) {
                patchModifier = new PatchModifiers();
                patchModifiers.Add(name, patchModifier);
            }
            return patchModifier;
        }
    }
    
    public class WriteModifiers
    {
        public  readonly    Dictionary<string, WriteModifier>    writes    = new Dictionary<string, WriteModifier>();
        
        internal void ModifyWrites(Dictionary<string, EntityValue> entities) {
            var modifications = new Dictionary<string, EntityValue>();
            foreach (var pair in entities) {
                var key = pair.Key;
                if (writes.TryGetValue(key, out var modifier)) {
                    var value       = pair.Value;
                    var modified    = modifier (value);
                    modifications.Add(key, modified);
                }
            }
            foreach (var pair in modifications) {
                var key     = pair.Key;
                var value   = pair.Value;
                entities[key] = value;
            }
        }
    }
    
    public class PatchModifiers
    {
        public  readonly    Dictionary<string, PatchModifier>    patches    = new Dictionary<string, PatchModifier>();
        
        internal void ModifyPatches(Dictionary<string, EntityPatch> entityPatches) {
            var modifications = new Dictionary<string, EntityPatch>();
            foreach (var pair in entityPatches) {
                var key = pair.Key;
                if (patches.TryGetValue(key, out var modifier)) {
                    EntityPatch value       = pair.Value;
                    EntityPatch modified    = modifier (value);
                    modifications.Add(key, modified);
                }
            }
            foreach (var pair in modifications) {
                var         key     = pair.Key;
                EntityPatch value   = pair.Value;
                entityPatches[key] = value;
            }
        }
    }
}
