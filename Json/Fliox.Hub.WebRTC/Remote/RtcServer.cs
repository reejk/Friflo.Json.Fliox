// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.WebRTC.Impl;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC
{
    public sealed class RtcHostConfig {
        public  string          SignalingDB     { get; init; } = "signaling";
        public  string          SignalingHost   { get; init; }
        public  string          User            { get; init; }
        public  string          Token           { get; init; }
        public  WebRtcConfig    WebRtcConfig    { get; init; }
    }
    
    public sealed class RtcServer : IHost
    {
        private readonly    WebRtcConfig                           config;
        private readonly    Signaling                              signaling;
        private readonly    Dictionary<ShortString, RtcSocketHost> clients;
        private readonly    IHubLogger                             logger;
        private const       string                                 LogName = "RTC-host";
        
        public RtcServer (RtcHostConfig rtcConfig, SharedEnv env = null) {
            var sharedEnv   = env  ?? SharedEnv.Default;
            logger          = sharedEnv.Logger;
            config          = rtcConfig.WebRtcConfig;
            clients         = new Dictionary<ShortString, RtcSocketHost>(ShortString.Equality);
            var signalingHub= new WebSocketClientHub (rtcConfig.SignalingDB, rtcConfig.SignalingHost, env);
            signaling       = new Signaling(signalingHub) { UserId = rtcConfig.User, Token = rtcConfig.Token };
            logger.Log(HubLog.Info, $"{LogName}: listening at: {rtcConfig.SignalingHost} db: '{rtcConfig.SignalingDB}'");
            LogInfo         = message => logger.Log(HubLog.Info, message); 
        }
        
        private event Action<string> LogInfo; 
        
        private void LogError(string message, Exception exception = null) {
            logger.Log(HubLog.Error, message, exception); 
        }

        public async Task AddHost(string hostId, HttpHost host)
        {
            LogInfo?.Invoke($"{LogName}: add host: '{hostId}'");
            signaling.SubscribeMessage<Offer>(nameof(Offer), (message, context) => {
                ProcessOffer(host, message);
            });
            signaling.SubscribeMessage<ClientIce>(nameof(ClientIce), (message, context) => {
                ProcessIceCandidate(message, context);
            });
            signaling.AddHost(new AddHost { hostId = hostId });
            await signaling.SyncTasks().ConfigureAwait(false);
        }
        
        private async void ProcessOffer(HttpHost host, Message<Offer> message) {
            if (!message.GetParam(out var offer, out var error)) {
                LogError($"{LogName}: invalid Offer. error: {error}");
                return;
            }
            var pc          = new PeerConnection(config);
            var socketHost  = new RtcSocketHost(pc, offer.client.ToString(), host.hub, this);
            clients.Add(offer.client, socketHost);
            
            // --- add peer connection event callbacks
            pc.OnConnectionStateChange += (state) => {
                LogInfo?.Invoke($"{LogName}: connection state change: {state}");
            };
            // var channelOpen = new TaskCompletionSource<bool>();
            // var dc = await pc.CreateDataChannel("test").ConfigureAwait(false); // right after connection creation. Otherwise: NoRemoteMedia
            pc.OnDataChannel += (remoteDc) => {
                socketHost.remoteDc = remoteDc; // note: remoteDc != dc created bellow
                remoteDc.OnMessage += (data) => socketHost.OnMessage(data);
                remoteDc.OnError += dcError   => { LogError($"{LogName}: datachannel onerror: {dcError}"); };
                LogInfo?.Invoke($"{LogName}: add data channel. label: {remoteDc.Label}");
                _ = socketHost.SendReceiveMessages();
                // channelOpen.SetResult(true);  
            };
            pc.OnIceCandidate += candidate => {
                // send ICE candidate to WebRTC client
                var jsonCandidate   = new JsonValue(candidate.ToJson());
                var hostIce         = new HostIce { client = offer.client, candidate = jsonCandidate };
                var msg             = signaling.SendMessage(nameof(HostIce), hostIce);
                msg.EventTargetClient(offer.client);
                _ = signaling.SyncTasks();
            };
            
            LogInfo?.Invoke($"{LogName}: --- SetRemoteDescription");
            var rtcOffer = new SessionDescription { type = SdpType.offer, sdp = offer.sdp };
            var descError = await pc.SetRemoteDescription(rtcOffer).ConfigureAwait(false);
            if (descError != null) {
                LogError($"{LogName}: setRemoteDescription failed. error: {descError}");
                return;
            }
            foreach (var candidate in socketHost.iceCandidates) {
                LogInfo?.Invoke($"{LogName}: --- AddIceCandidate");
                pc.AddIceCandidate(candidate);
            }
            socketHost.iceCandidates.Clear();
            var answer = await pc.CreateAnswer().ConfigureAwait(false);
            await pc.SetLocalDescription(answer).ConfigureAwait(false);
            
            // await channelOpen.Task.ConfigureAwait(false);
            
            // --- send answer SDP -> Signaling Server
            var answerSDP = new Answer { client = offer.client, sdp = answer.sdp };
            var answerMsg = signaling.SendMessage(nameof(Answer), answerSDP);
            answerMsg.EventTargetClient(offer.client);
            await signaling.SyncTasks().ConfigureAwait(false);
        }
        
        private void ProcessIceCandidate(Message<ClientIce> message, EventContext context) {
            if (!message.GetParam(out var clientIce, out var error)) {
                LogError($"{LogName}: invalid client ICE candidate. error: {error}");
                return;                    
            }
            if (!clients.TryGetValue(clientIce.client, out var socketHost)) {
                LogError($"{LogName}: client not found. client: {clientIce.client}");
                return;
            }
            var parseCandidate= IceCandidate.TryParse(clientIce.candidate.AsString(), out var iceCandidate);
            if (!parseCandidate) {
                LogError($"{LogName}: invalid ICE candidate");
                return;
            }
            LogInfo?.Invoke($"{LogName}: received ICE candidate. client: {clientIce.client}");
            var signalingState = socketHost.pc.SignalingState;
            if (signalingState == SignalingState.HaveRemoteOffer || signalingState == SignalingState.Stable) {
                LogInfo?.Invoke($"{LogName}: --- AddIceCandidate");
                socketHost.pc.AddIceCandidate(iceCandidate);
            } else {
                socketHost.iceCandidates.Add(iceCandidate);
            }
        }
    }
}
