// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.SQL;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note!  Keep file in sync with:  FlioxHub-sync.cs

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    public partial class FlioxHub
    {
        /// <summary>
        /// Execute all <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/> send by client.<br/>
        /// Method is thread-safe as multiple <see cref="Client.FlioxClient"/> instances are allowed to
        /// use a single <see cref="FlioxHub"/> instance.
        /// </summary>
        /// <remarks>
        /// All requests to a <see cref="FlioxHub"/> are handled by this method.
        /// By design this is the 'front door' all requests have to pass to get processed.
        /// <para>
        ///   <see cref="ExecuteRequestAsync"/> catches exceptions thrown by a <see cref="SyncRequestTask"/> but 
        ///   this is only a fail safe mechanism.
        ///   Thrown exceptions need to be handled by proper error handling in the first place.
        ///
        ///   Reasons for the design decision: 
        ///   <para> a) Without a proper error handling the root cause of an error cannot be traced back.</para>
        ///   <para> b) Exceptions are expensive regarding CPU utilization and heap allocation.</para>
        /// </para>
        /// <para>
        ///   An exception can have two different reasons:
        ///   <para> 1. The implementation of an <see cref="EntityContainer"/> is missing a proper error handling.
        ///          A proper error handling requires to set a meaningful <see cref="Protocol.Models.TaskExecuteError"/> to
        ///          <see cref="Protocol.Models.ITaskResultError.Error"/></para>
        ///   <para> 2. An issue in the namespace <see cref="Friflo.Json.Fliox.Hub.Protocol"/> which must to be fixed.</para> 
        /// </para>
        /// </remarks>
        public virtual async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext)
        {
            switch (syncRequest.intern.executionType) {
                case Error: return new ExecuteSyncResult(syncRequest.intern.error, ErrorResponseType.BadRequest);
                case Sync:  break;
                case Async: break;
                default:    return new ExecuteSyncResult($"Invalid execution type: {syncRequest.intern.executionType}", ErrorResponseType.Exception);
            }
            syncContext.request             = syncRequest;
            if (syncContext.authState.authExecuted) throw new InvalidOperationException("Expect AuthExecuted == false");
            if (authenticator.IsSynchronous(syncRequest)) {
                      authenticator.Authenticate     (syncRequest, syncContext);
            } else {
                await authenticator.AuthenticateAsync(syncRequest, syncContext).ConfigureAwait(false);
            }
            if (syncRequest.intern.error != null) {
                return new ExecuteSyncResult (syncRequest.intern.error, ErrorResponseType.BadRequest); 
            }
            syncContext.hub                 = this;
            var db =  syncContext.database  = syncRequest.intern.db;
            syncContext.clientId            = syncRequest.clientId;
            syncContext.clientIdValidation  = authenticator.ValidateClientId(clientController, syncContext);
            
            var service         = db.service;
            var requestTasks    = syncRequest.tasks;
            var taskCount       = requestTasks.Count;

            service.PreExecuteTasks(syncContext);
            
            syncContext.syncPools?.Reuse();

            var response        = SyncResponse.Create(syncContext, taskCount);
            syncContext.response= response;
            response.database   = syncRequest.database;
            var tasks           = response.tasks;
            
            // ------------------------ loop through all given tasks and execute them ------------------------
            for (int index = 0; index < taskCount; index++) {
                var task = requestTasks[index];
                try {
                    // Execute task synchronous or asynchronous.
                    SyncTaskResult result;
                    if (task.intern.executionType == ExecutionType.Sync) {
                        result =       service.ExecuteTask      (task, db, response, syncContext);
                    } else {
                        result = await service.ExecuteTaskAsync (task, db, response, syncContext).ConfigureAwait(false);
                    }
                    tasks.Add(result);
                } catch (Exception e) {
                    tasks.Add(TaskExceptionError(e)); // Note!  Should not happen - see documentation of this method.
                    var message = GetLogMessage(db.name, syncRequest.userId, index, task);
                    Logger.Log(HubLog.Error, message, e);
                }
            }
            if (syncContext.IsTransactionPending) {
                await syncContext.Transaction(TransCommand.Commit, taskCount).ConfigureAwait(false);
            }
            syncContext.ReturnConnection();
            PostExecute(syncRequest, response, syncContext);
            return new ExecuteSyncResult(response);
        }
    }
}
