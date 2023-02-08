﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote.Transport.Udp
{
    /// <summary>
    /// Implementation aligned with <see cref="WebSocketHost"/>
    /// </summary>
    /// <remarks>
    /// Counterpart of <see cref="UdpSocketClientHub"/> used by the server.<br/>
    /// </remarks>
    internal sealed class UdpSocketSyncHost : SocketHost
    {
        internal readonly   IPEndPoint      endpoint;
        private  readonly   UdpServerSync   server;

        internal UdpSocketSyncHost (UdpServerSync server, IPEndPoint endpoint)
        : base (server.hub)
        {
            this.server     = server;
            this.endpoint   = new IPEndPointReuse(endpoint.Address, endpoint.Port);
        }
        
        // --- IEventReceiver
        protected internal override bool    IsRemoteTarget ()   => true;
        protected internal override bool    IsOpen ()           => true;
        
        // --- WebHost
        protected override void SendMessage(in JsonValue message) {
            server.sendQueue.AddTail(message, new UdpMeta(endpoint));
        }
    }
}