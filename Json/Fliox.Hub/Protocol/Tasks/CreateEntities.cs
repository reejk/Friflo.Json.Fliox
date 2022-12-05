﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Create the given <see cref="entities"/> in the specified <see cref="container"/>
    /// </summary>
    public sealed class CreateEntities : SyncRequestTask
    {
        /// <summary>container name the <see cref="entities"/> are created</summary>
        [Required]  public  string              container;
        [Ignore]   internal SmallString         containerCmp;
                    public  Guid?               reservedToken;
        /// <summary>name of the primary key property in <see cref="entities"/></summary>
                    public  string              keyName;
        /// <summary>the <see cref="entities"/> which are created in the specified <see cref="container"/></summary>
        [Required]  public  List<JsonEntity>    entities;
                        
        public   override   TaskType            TaskType => TaskType.create;
        public   override   string              TaskName => $"container: '{container}'";
        
        private bool PrepareCreate(
            EntityContainer         entityContainer,
            SyncContext             syncContext,
            ref List<EntityError>   validationErrors,
            out TaskErrorResult     error)
        {
            if (container == null) {
                error = MissingContainer();
                return false;
            }
            if (entities == null) {
                error = MissingField(nameof(entities));
                return false;
            }
            if (!EntityUtils.GetKeysFromEntities(keyName, entities, syncContext, out string errorMsg)) {
                error = InvalidTask(errorMsg);
                return false;
            }
            containerCmp = new SmallString(container);

            errorMsg = entityContainer.database.Schema?.ValidateEntities (container, entities, syncContext, EntityErrorType.WriteError, ref validationErrors);
            if (errorMsg != null) {
                error = TaskError(new CommandError(TaskErrorResultType.ValidationError, errorMsg));
                return false;
            }
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooled = syncContext.pool.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooled.instance;
                    for (int n = 0; n < entities.Count; n++) {
                        var entity = entities[n];
                        // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                        entities[n] = new JsonEntity(entity.key, patcher.Copy(entity.value, true));
                    }
                }
            }
            error = null;
            return true;
        }
        
        public override async Task<SyncTaskResult> ExecuteAsync(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = database.GetOrCreateContainer(container);
            List<EntityError> validationErrors = null;
            if (!PrepareCreate(entityContainer, syncContext, ref validationErrors, out var error)) {
                return error;
            }
            var result = await entityContainer.CreateEntitiesAsync(this, syncContext).ConfigureAwait(false);
            
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
        
        public override SyncTaskResult Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var entityContainer = database.GetOrCreateContainer(container);
            List<EntityError> validationErrors = null;
            if (!PrepareCreate(entityContainer, syncContext, ref validationErrors, out var error)) {
                return error;
            }
            var result = entityContainer.CreateEntities(this, syncContext);
            
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            SyncResponse.AddEntityErrors(ref result.errors, validationErrors);
            return result;
        }
    }

    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="CreateEntities"/> task
    /// </summary>
    public sealed class CreateEntitiesResult : SyncTaskResult, ICommandResult
    {
        [Ignore]    public CommandError        Error { get; set; }
        /// <summary>list of entity errors failed to create</summary>
                    public List<EntityError>   errors;
        
        internal override  TaskType            TaskType => TaskType.create;
    }
}