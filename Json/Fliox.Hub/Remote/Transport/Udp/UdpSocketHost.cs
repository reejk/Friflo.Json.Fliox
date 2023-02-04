﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Net;

namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Implementation aligned with <see cref="WebSocketHost"/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpSocketClientHub"/> used by the server.<br/>
    /// </remarks>
    internal sealed class UdpSocketHost : SocketHost
    {
        internal readonly   IPEndPoint  remoteClient;
        private  readonly   UdpServer   server;

        internal UdpSocketHost (UdpServer server, IPEndPoint  remoteClient)
        : base (server.hub, server.hostEnv)
        {
            this.server         = server;
            this.remoteClient   = remoteClient;
        }
        
        // --- IEventReceiver
        public override bool    IsRemoteTarget ()   => true;
        public override bool    IsOpen ()           => true;
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            server.sendQueue.AddTail(message, new UdpMeta(remoteClient));
        }
    }
}