﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database
{
    // ----------------------------------------- EntityDatabase -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public abstract class EntityDatabase : IDisposable
    {
        // [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<string, EntityContainer>    containers = new Dictionary<string, EntityContainer>();
        
        public abstract EntityContainer CreateContainer(string name, EntityDatabase database);

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }

        internal void AddContainer(EntityContainer container)
        {
            containers.Add(container.name, container);
        }
        
        public bool TryGetContainer(string name, out EntityContainer container)
        {
            return containers.TryGetValue(name, out container);
        }

        public EntityContainer GetOrCreateContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        public virtual async Task<SyncResponse> Execute(SyncRequest syncRequest) {
            var response = new SyncResponse {
                tasks    = new List<TaskResult>(syncRequest.tasks.Count),
                results  = new Dictionary<string, ContainerEntities>()
            };
            foreach (var task in syncRequest.tasks) {
                try {
                    var result = await task.Execute(this, response);
                    response.tasks.Add(result);
                }
                catch (Exception e) {
                    var exceptionName = e.GetType().Name;
                    var result = new TaskError{
                        type    = TaskErrorType.UnhandledException,
                        message = $"{exceptionName}: {e.Message}"
                    };
                    response.tasks.Add(result);
                }
            }
            response.AssertResponse(syncRequest);
            return response;
        }
    }
}
