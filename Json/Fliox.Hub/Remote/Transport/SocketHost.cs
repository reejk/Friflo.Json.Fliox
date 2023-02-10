// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Hub.Remote.Tools;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Pools;
using Friflo.Json.Fliox.Utils;
using static Friflo.Json.Fliox.Hub.Host.ExecutionType;

// Note! - Must not have any dependency to System.Net, System.Net.Sockets, System.Net.WebSockets, System.Net.Http
//         or other specific communication implementations.

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    /// <summary>
    /// Counterpart of <see cref="SocketClientHub"/> used by socket implementations running on the server.<br/>
    /// <br/>
    /// <see cref="SocketHost"/> provide a set of methods to:<br/>
    /// - parse serialized <see cref="SyncRequest"/> messages.<br/>
    /// - execute <see cref="SyncRequest"/>'s and send serialized <see cref="SyncResponse"/> to client<br/>
    /// </summary>
    /// <remarks>
    /// The implementation aims to prevent <b>head-of-line blocking</b>.<br/>
    /// Which means it supports <b>out-of-order delivery</b> for responses send to clients.<br/>
    /// <br/>
    /// <b>Special case</b><br/>
    /// If enabling queueing request in <see cref="DatabaseService"/>
    /// <b>head-of-line blocking</b> can occur in case request execution is not 'instant'<br/>
    /// <br/>
    /// Requests can be executed synchronous, asynchronous or on a specific thread / execution context.<br/>
    /// - If <see cref="DatabaseService"/> is configured to queue requests they are executed
    ///   on the thread calling <see cref="DatabaseService.queue"/> <br/>
    /// - Synchronous in case a request can be executed synchronous<br/>
    /// - Asynchronous in case a request requires asynchronous execution<br/>
    /// </remarks>
    public abstract class SocketHost : EventReceiver
    {
        private   readonly  FlioxHub                    hub;
        private   readonly  TypeStore                   typeStore;
        private   readonly  SharedEnv                   sharedEnv;
        private   readonly  ObjectPool<ReaderPool>      readerPool;
        private   readonly  ObjectMapper                readMapper;
        private   readonly  ObjectPool<ObjectMapper>    objectPool;
        private   readonly  bool                        useReaderPool;
        private   readonly  Stack<SyncContext>          syncContextPool;
        protected readonly  IHubLogger                  logger;
        
        protected abstract  void        SendMessage(in JsonValue message);

        protected SocketHost(FlioxHub hub) {
            var env         = hub.sharedEnv;
            sharedEnv       = env;
            typeStore       = sharedEnv.TypeStore;
            this.hub        = hub;
            logger          = hub.Logger;
            var pool        = sharedEnv.Pool;
            readerPool      = pool.ReaderPool;
            objectPool      = pool.ObjectMapper;
            readMapper      = objectPool.Get().instance;
            useReaderPool   = hub.GetFeature<RemoteHostEnv>().useReaderPool;
            syncContextPool = new Stack<SyncContext>();
        }

        protected internal override void SendEvent(in ClientEvent clientEvent) {
            try {
                SendMessage(clientEvent.message);
            }
            catch (Exception e) {
                logger.Log(HubLog.Error, "WebSocketHost.SendEvent", e);
            }
        }
        
        /// <summary>
        /// Method is not thread-safe<br/>
        /// Expectation is method is called sequentially from the receive message loop- 
        /// </summary>
        private SyncRequest ParseRequest(in JsonValue request) {
            var reader = readMapper.reader;
            if (useReaderPool) {
                reader.ReaderPool = readerPool.Get().instance.Reuse();
            }
            var syncRequest = RemoteMessageUtils.ReadSyncRequest(reader, request, out string error);
            if (error == null) {
                return syncRequest;
            }
            var response = JsonResponse.CreateError(readMapper.writer, error, ErrorResponseType.BadRequest, null);
            SendMessage(response.body);
            return null;
        }
        
        /// <summary>create or use a pooled <see cref="SyncContext"/></summary>
        private SyncContext CreateSyncContext() {
            SyncContext syncContext;
            lock (syncContextPool) {
                if (syncContextPool.TryPop(out syncContext)) {
                    return syncContext;
                }
            }
            var syncPools       = new SyncPools(typeStore);
            var syncBuffers     = new SyncBuffers(new List<SyncRequestTask>(), new List<SyncRequestTask>(), new List<JsonValue>());
            var memoryBuffer    = new MemoryBuffer(4 * 1024);
            syncContext         = new SyncContext(sharedEnv, this, syncBuffers, syncPools); // reused context
            syncContext.SetMemoryBuffer(memoryBuffer);
            return syncContext;
        }
        
        /// <summary>
        /// <b>Note</b> <br/>
        /// Return <see cref="SyncContext"/> to pool after calling <see cref="SendResponse"/> or <see cref="SendResponseException"/>
        /// as <see cref="SyncContext.MemoryBuffer"/> could be used when writing <see cref="JsonResponse"/>.
        /// </summary>
        private void ReturnSyncContext(SyncContext syncContext) {
            var memoryBuffer = syncContext.MemoryBuffer;
            syncContext.Init();
            syncContext.SetMemoryBuffer(memoryBuffer);
            lock (syncContextPool) {
                syncContextPool.Push(syncContext);
            }
        }
        
        internal void OnReceive(in JsonValue request, ref SocketMetrics metrics)
        {
            // --- precondition: message was read from socket
            try {
                // --- 1. Parse request
                Interlocked.Increment(ref metrics.receivedCount);
                var t1          = Stopwatch.GetTimestamp();
                var syncRequest = ParseRequest(request);
                var t2          = Stopwatch.GetTimestamp();
                Interlocked.Add(ref metrics.requestReadTime, t2 - t1);
                if (syncRequest == null) {
                    return;
                }
                // --- 2. Execute request
                ExecuteRequest (syncRequest);
                var t3          = Stopwatch.GetTimestamp();
                Interlocked.Add(ref metrics.requestExecuteTime, t3 - t2);
            }
            catch (Exception e) {
                SendResponseException(e, null);
            }
        }
        
        /// <summary>
        /// Method is not thread-safe<br/>
        /// Expectation is method is called sequentially from the receive message loop- 
        /// </summary>
        private void ExecuteRequest(SyncRequest syncRequest)
        {
            var syncContext = CreateSyncContext();

            syncContext.MemoryBuffer.Reset();
            var reqId   = syncRequest.reqId;
            var pool    = readMapper.reader.ReaderPool;
            try {
                var executionType = hub.InitSyncRequest(syncRequest);
                Task<ExecuteSyncResult> syncResultTask;
                switch (executionType) {
                    case Async: syncResultTask  = hub.ExecuteRequestAsync (syncRequest, syncContext); break;
                    case Queue: syncResultTask  = hub.QueueRequestAsync   (syncRequest, syncContext); break;
                    default:
                        var syncResult          = hub.ExecuteRequest      (syncRequest, syncContext);
                        if (pool != null) readerPool.Return(pool);
                        SendResponse(syncResult, reqId);
                        ReturnSyncContext(syncContext);
                        return;
                }
                syncResultTask.ContinueWith(task => {
                    if (pool != null) readerPool.Return(pool);
                    SyncResultContinuation(task, reqId);
                    ReturnSyncContext(syncContext);
                });
            }
            catch (Exception e) {
                SendResponseException(e, reqId);
                ReturnSyncContext(syncContext);
            }
        }
        
        private void SyncResultContinuation(Task<ExecuteSyncResult> task, int? reqId) {
            var status = task.Status;
            switch (status) {
                case TaskStatus.RanToCompletion:
                    var syncResult = task.Result;
                    SendResponse(syncResult, reqId);
                    return;
                case TaskStatus.Faulted:
                    var exception = task.Exception;
                    SendResponseException(exception, reqId);
                    return;
                case TaskStatus.Canceled:
                    var canceledException = task.Exception; // OperationCanceledException
                    SendResponseException(canceledException, reqId);
                    return;
                default:
                    var errorMsg = $"unexpected continuation task status {status}, reqId: {reqId}";
                    logger.Log(HubLog.Error, errorMsg);
                    Debug.Fail(errorMsg);
                    return;
            }
        }
        
        private void SendResponse(in ExecuteSyncResult syncResult, int? reqId) {
            var error   = syncResult.error;
            var mapper  = objectPool.Get().instance;
            var writer  = RemoteMessageUtils.GetCompactWriter(mapper);
            if (error != null) {
                var errorResponse = JsonResponse.CreateError(writer, error.message, error.type, reqId);
                SendMessage(errorResponse.body);
            } else {
                var response = RemoteHostUtils.CreateJsonResponse(syncResult, reqId, writer);
                SendMessage(response.body);
            }
            objectPool.Return(mapper);
        }
        
        private void SendResponseException(Exception e, int? reqId) {
            var errorMsg    = ErrorResponse.ErrorFromException(e).ToString();
            var mapper      = objectPool.Get().instance;
            var response    = JsonResponse.CreateError(mapper.writer, errorMsg, ErrorResponseType.Exception, reqId);
            objectPool.Return(mapper);
            SendMessage(response.body);
        }
    }
}