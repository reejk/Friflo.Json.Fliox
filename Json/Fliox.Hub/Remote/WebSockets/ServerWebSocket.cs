// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ConvertToAutoProperty
namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    internal sealed class ServerWebSocket : WebSocket
    {
        private readonly    NetworkStream           stream;
        private readonly    FrameProtocolReader     reader      = new FrameProtocolReader();
        private readonly    FrameProtocolWriter     writer      = new FrameProtocolWriter(false); // server must not mask payloads
        private readonly    SemaphoreSlim           sendLock    = new SemaphoreSlim(1);
        //
        private             WebSocketCloseStatus?   closeStatus;
        private             string                  closeStatusDescription;
        private             WebSocketState          state;
        private             string                  subProtocol = null;

        public  override    WebSocketCloseStatus?   CloseStatus             => closeStatus;
        public  override    string                  CloseStatusDescription  => closeStatusDescription;
        public  override    WebSocketState          State                   => state;
        public  override    string                  SubProtocol             => subProtocol;
        
        public override void Abort() {
            throw new NotImplementedException();
        }

        public override async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            await writer.CloseAsync (stream, closeStatus, "client closed connection", cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                
            sendLock.Release();

            Close();
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }

        public override void Dispose() {
            sendLock.Dispose();
            stream.Dispose();
        }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> dataBuffer, CancellationToken cancellationToken) {
            if (await reader.ReadFrame(stream, dataBuffer, cancellationToken).ConfigureAwait(false)) {
                return new WebSocketReceiveResult(reader.ByteCount, reader.MessageType, reader.EndOfMessage);
            }
            state                   = reader.SocketState;
            closeStatus             = reader.CloseStatus;
            closeStatusDescription  = reader.CloseStatusDescription;
            
            return new WebSocketReceiveResult(reader.ByteCount, reader.MessageType, reader.EndOfMessage, closeStatus, closeStatusDescription);
        }

        public override async Task SendAsync(ArraySegment<byte> dataBuffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) {
            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            await writer.WriteAsync(stream, dataBuffer, messageType, endOfMessage, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false); // todo required?
            
            sendLock.Release();
        }
        // ---------------------------------------------------------------------------------------------

        internal ServerWebSocket(NetworkStream stream) {
            state       = WebSocketState.Open;
            this.stream = stream;
        }
        
        private void Close() {
            stream.Close();
            // stream does not close underlying socket => close it explicit
            var flags       = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var socketInfo  = typeof(NetworkStream).GetProperty("Socket", flags);
            var socket      = (Socket)socketInfo.GetValue(stream); // HttpConnection
            socket.Close();
        }
    }
}