﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database.Event;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Flow.Database.Auth
{
    // todo untested
    public class ClientAuthenticator
    {
        private readonly ConcurrentDictionary<IEventTarget, ClientCredentials>  credByTarget;
        private readonly ConcurrentDictionary<string,       ClientCredentials>  credByClient;
        
        public ClientAuthenticator() {
            credByTarget    = new ConcurrentDictionary<IEventTarget, ClientCredentials>();
            credByClient    = new ConcurrentDictionary<string,       ClientCredentials>();
        }
        
        public async ValueTask Authenticated(SyncRequest syncRequest, MessageContext messageContext)
        {
            var clientId = syncRequest.clientId;
            if (clientId == null) {
                messageContext.authState.SetFailed("authorization requires: clientId");
                return;
            }
            var eventTarget = messageContext.eventTarget;
            // already authorized?
            if (eventTarget != null && credByTarget.ContainsKey(eventTarget)) {
                messageContext.authState.SetSuccess();
                return;
            }
            var token = syncRequest.token;
            if (token == null) {
                messageContext.authState.SetFailed("authorization requires: token");
                return;
            }
            if (!credByClient.TryGetValue(clientId, out var credential)) {
                messageContext.authState.SetFailed($"client not authorized. Invalid token. clientId: '{clientId}'");
                return;
            }
            if (token != credential.token) {
                messageContext.authState.SetFailed($"client not authorized. Invalid token. clientId: '{clientId}'");
                return;
            }
            // Update target if changed for early out when already authorized.
            if (credential.target != eventTarget) {
                if (credential.target != null) {
                    credByTarget.TryRemove(credential.target, out _);
                    credential.target = eventTarget;
                    credByTarget.TryAdd(eventTarget, credential);
                }
            }
            messageContext.authState.SetSuccess();
        }
    }
    
    internal class ClientCredentials
    {
        internal string         token;
        internal IEventTarget   target;
    }
}