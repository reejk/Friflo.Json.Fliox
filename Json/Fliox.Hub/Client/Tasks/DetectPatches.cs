﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public sealed class DetectPatchesTask : SyncTask
    {
        public              IReadOnlyList<EntityPatchInfo>  Patches     => patches;
        public              string                          Container   => syncSet.EntitySet.name;
        [DebuggerBrowsable(Never)]
        internal readonly   List<EntityPatchInfo>           patches;
        internal readonly   SyncSet                         syncSet;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState                       state;
        internal override   TaskState                       State   => state;
        public   override   string                          Details => $"DetectPatchesTask (container: {Container}, patches: {patches.Count})";
        

        internal DetectPatchesTask(SyncSet syncSet) {
            patches         = new List<EntityPatchInfo>();
            this.syncSet    = syncSet;
        }

        internal void AddPatch(EntityPatch entityPatch) {
            if (entityPatch.id.IsNull())
                throw new ArgumentException("id must not be null");
            var patch = new EntityPatchInfo(entityPatch);
            patches.Add(patch);
        }
        
        internal void SetResult() {
            var entityErrorInfo = new TaskErrorInfo();
            foreach (var patch in patches) {
                if (syncSet.errorsPatch.TryGetValue(patch.entityPatch.id, out EntityError error)) {
                    entityErrorInfo.AddEntityError(error);
                }
            }
            if (entityErrorInfo.HasErrors) {
                state.SetError(entityErrorInfo);
            } else {
                state.Executed = true;
            }
        }
    }
}
