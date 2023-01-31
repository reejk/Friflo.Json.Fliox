﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote.Udp
{
    /// <summary>
    /// Each <see cref="WebSocketConnection"/> store its send requests in the <see cref="requestMap"/>
    /// to map received response messages to its related <see cref="SyncRequest"/>
    /// </summary>
    internal sealed class UdpSocketConnection
    {
        internal  readonly  UdpClient           websocket   = new UdpClient();
        internal  readonly  RemoteRequestMap    requestMap  = new RemoteRequestMap();
    }


    /// <summary>
    /// A <see cref="FlioxHub"/> accessed remotely  using a <see cref="UdpClient"/> connection<br/>
    /// Initial implementation based on <see cref="WebSocketClientHub"/>
    /// </summary>
    public sealed class UdpSocketClientHub : SocketClientHub
    {
        private  readonly   string                      endpoint;
        private  readonly   Uri                         endpointUri;
        /// Incrementing requests id used to map a <see cref="ProtocolResponse"/>'s to its related <see cref="SyncRequest"/>.
        private             int                         reqId;
        public              bool                        IsConnected => true;

        private             UdpSocketConnection         wsConnection;
        private             byte[]                      sendBuffer;
        
        private  readonly   CancellationTokenSource     cancellationToken = new CancellationTokenSource();
        
        public   override   string                      ToString() => $"{database.name} - endpoint: {endpoint}";
        
        public UdpSocketClientHub(string dbName, string endpoint, SharedEnv env = null, RemoteClientAccess access = RemoteClientAccess.Multi)
            : base(new RemoteDatabase(dbName), env, access)
        {
            this.endpoint   = endpoint;
            endpointUri     = new Uri(endpoint);
            sendBuffer      = new byte[128];
        }
        
        /* public override void Dispose() {
            base.Dispose();
            // websocket.CancelPendingRequests();
        } */
        
        /* private WebSocketConnection GetWebsocketConnection() {
            lock (websocketLock) {
                var wsConn = wsConnection;
                if (wsConn != null && wsConn.websocket.State == WebSocketState.Open)
                    return wsConn;
                return null;
            }
        }
        
        private Task<WebSocketConnection> JoinConnects(out TaskCompletionSource<WebSocketConnection> tcs, out WebSocketConnection wsConn) {
            lock (websocketLock) {
                if (connectTask != null) {
                    wsConn  = null;
                    tcs     = null;
                    return connectTask;
                }
                wsConn  = wsConnection = new WebSocketConnection();
                tcs     = new TaskCompletionSource<WebSocketConnection>();
                connectTask = tcs.Task;
                return connectTask;
            }
        }
        
        // static int count;
        
        private async Task<WebSocketConnection> Connect() {
            var task = JoinConnects(out var tcs, out WebSocketConnection wsConn);
            if (tcs == null) {
                wsConn = await task.ConfigureAwait(false);
                return wsConn;
            }

            // Console.WriteLine($"WebSocketClientHub.Connect() endpoint: {endpoint}");
            // ws.Options.SetBuffer(4096, 4096);
            try {
                // Console.WriteLine($"Connect {++count}");
                await wsConn.websocket.ConnectAsync(endpointUri, CancellationToken.None).ConfigureAwait(false);

                connectTask = null;
                tcs.SetResult(wsConn);
            } catch (Exception e) {
                connectTask = null;
                tcs.SetException(e);
                throw;
            }
            try {
                _ = RunReceiveMessageLoop(wsConn).ConfigureAwait(false);
            } catch (Exception e) {
                Debug.Fail("ReceiveLoop() failed", e.Message);
            }
            return wsConn;
        }
        
        public async Task Close() {
            WebSocketConnection wsConn;
            lock (websocketLock) {
                wsConn = wsConnection;
                if (wsConn == null || wsConn.websocket.State == WebSocketState.Closed)
                    return;
                wsConnection = null;
            }
            await wsConn.websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
        } */
        
        private async Task RunReceiveMessageLoop(UdpSocketConnection wsConn) {
            using (var mapper = new ObjectMapper(sharedEnv.TypeStore)) {
                await ReceiveMessageLoop(wsConn, mapper.reader).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// In contrast to <see cref="WebSocketHost"/> the <see cref="WebSocketClientHub"/> has no SendMessageLoop() <br/>
        /// This is possible because WebSocket messages are only response messages created in this loop. <br/>
        /// As <see cref="ReceiveMessageLoop"/> is called sequentially in the loop, WebSocket.SendAsync() is called only once at any time.
        /// Infos: <br/>
        /// - A blocking WebSocket.SendAsync() call does not block WebSocket.ReceiveAsync() <br/>
        /// - The created <see cref="RemoteRequest.response"/>'s act as a queue. <br/>
        /// </summary>
        private async Task ReceiveMessageLoop(UdpSocketConnection wsConn, ObjectReader reader) {
            var parser          = new Utf8JsonParser();
            var ws              = wsConn.websocket;
            var memoryStream    = new MemoryStream();
            while (true)
            {
                memoryStream.Position = 0;
                memoryStream.SetLength(0);
                try {
                    // --- read complete WebSocket message

                    UdpReceiveResult wsResult = await ws.ReceiveAsync().ConfigureAwait(false);
                    var buffer = wsResult.Buffer;
                    memoryStream.Write(buffer, 0, buffer.Length);

                    // --- determine message type
                    var message     = new JsonValue(memoryStream.GetBuffer(), (int)memoryStream.Position);
                    var messageHead = RemoteUtils.ReadMessageHead(ref parser, message);
                    
                    // --- handle either response or event message
                    switch (messageHead.type) {
                        case MessageType.resp:
                        case MessageType.error:
                            if (!messageHead.reqId.HasValue)
                                throw new InvalidOperationException($"missing reqId in response:\n{message}");
                            var id = messageHead.reqId.Value;
                            if (!wsConn.requestMap.Remove(id, out RemoteRequest request)) {
                                throw new InvalidOperationException($"reqId not found. id: {id}");
                            }
                            reader.ReaderPool   = request.responseReaderPool;
                            var response        = reader.Read<ProtocolResponse>(message);
                            request.response.SetResult(response);
                            break;
                        case MessageType.ev:
                            var clientEvent = new ClientEvent (messageHead.dstClientId, message);
                            OnReceiveEvent(clientEvent);
                            break;
                    }
                }
                catch (Exception e)
                {
                    var message = $"WebSocketClientHub receive error: {e.Message}";
                    Logger.Log(HubLog.Error, message, e);
                    wsConn.requestMap.CancelRequests();
                }
            }
        }
        
        public override async Task<ExecuteSyncResult> ExecuteRequestAsync(SyncRequest syncRequest, SyncContext syncContext) {
            var wsConn = wsConnection;
            /* var wsConn = GetWebsocketConnection();
            if (wsConn == null) {
                wsConn = await Connect().ConfigureAwait(false);
            } */
            int sendReqId       = Interlocked.Increment(ref reqId);
            syncRequest.reqId   = sendReqId;

            try {
                using (var pooledMapper = syncContext.ObjectMapper.Get()) {
                    var writer              = pooledMapper.instance.writer;
                    writer.Pretty           = false;
                    writer.WriteNullMembers = false;
                    var rawRequest  = RemoteUtils.CreateProtocolMessage(syncRequest, writer);
                    // request need to be queued _before_ sending it to be prepared for handling the response.
                    var wsRequest   = new RemoteRequest(syncContext, cancellationToken);
                    wsConn.requestMap.Add(sendReqId, wsRequest);
                    
                    var length = rawRequest.Count;
                    if (sendBuffer.Length < length) {
                        sendBuffer = new byte[length];
                    }

                    // --- Send message
                    await wsConn.websocket.SendAsync(sendBuffer, length, null).ConfigureAwait(false);
                    
                    // --- Wait for response
                    var response = await wsRequest.response.Task.ConfigureAwait(false);
                    
                    if (response is SyncResponse syncResponse) {
                        return new ExecuteSyncResult(syncResponse);
                    }
                    if (response is ErrorResponse errorResponse) {
                        return new ExecuteSyncResult(errorResponse.message, errorResponse.type);
                    }
                    return new ExecuteSyncResult($"invalid response: Was: {response.MessageType}", ErrorResponseType.BadResponse);
                }
            }
            catch (Exception e) {
                var error = ErrorResponse.ErrorFromException(e);
                error.Append(" endpoint: ");
                error.Append(endpoint);
                var msg = error.ToString();
                return new ExecuteSyncResult(msg, ErrorResponseType.Exception);
            }
        }
    }
}
