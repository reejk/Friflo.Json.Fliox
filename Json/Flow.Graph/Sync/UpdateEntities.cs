﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    public class UpdateEntities : DatabaseTask, IDatabaseCommand
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
        
        public              DatabaseError       Error { get; set; }
        internal override   TaskType            TaskType => TaskType.Update;
        public   override   string              ToString() => "container: " + container;
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response) {
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                var patcher = entityContainer.SyncContext.jsonPatcher;
                foreach (var entity in entities) {
                    entity.Value.SetJson(patcher.Copy(entity.Value.Json, true));
                }
            }
            return await entityContainer.UpdateEntities(this);
        }
    }
    
    public class UpdateEntitiesResult : TaskResult
    {
        internal override   TaskType            TaskType => TaskType.Update;
    }
}