﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Flow.Sync
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public class SyncRequest
    {
        public  List<DatabaseTask>                      tasks;
    }
    
    public class SyncResponse
    {
        public  List<TaskResult>                        tasks;
        public  Dictionary<string, ContainerEntities>   results;
        public  Dictionary<string, EntityErrors>        createErrors;
        
        internal ContainerEntities GetContainerResult(string container) {
            if (results.TryGetValue(container, out ContainerEntities result))
                return result;
            result = new ContainerEntities {
                container = container,
                entities = new Dictionary<string,EntityValue>()
            };
            results.Add(container, result);
            return result;
        }
        
        internal EntityErrors GetCreateErrors(string container) {
            if (createErrors.TryGetValue(container, out EntityErrors result))
                return result;
            result = new EntityErrors {
                container   = container,
                errors      = new Dictionary<string,EntityError>()
            };
            createErrors.Add(container, result);
            return result;
        }

        public void AssertResponse(SyncRequest request) {
            var expect = request.tasks.Count;
            var actual = tasks.Count;
            if (expect != actual) {
                var msg = $"Expect response.task.Count == request.task.Count: expect: {expect}, actual: {actual}"; 
                throw new InvalidOperationException(msg);
            }
        }
    }
    
    // ------ ContainerEntities
    public class ContainerEntities
    {
        public  string                                  container; // only for debugging
        public  Dictionary<string, EntityValue>         entities;
        
        internal void AddEntities(Dictionary<string, EntityValue> add) {
            foreach (var entity in add) {
                entities.TryAdd(entity.Key, entity.Value);
            }
        }
    }
    
    public class EntityErrors
    {
        public  string                                  container; // only for debugging
        public  Dictionary<string, EntityError>         errors;
        
        internal void AddErrors(Dictionary<string, EntityError> errors) {
            foreach (var error in errors) {
                this.errors.TryAdd(error.Key, error.Value);
            }
        }
    }
}
