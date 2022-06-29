﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    public interface IEventReceiver {
        bool        IsOpen ();
        Task<bool>  ProcessEvent(ProtocolEvent ev);
    }
    
    /// <summary>
    /// An <see cref="EventDispatcher"/> is used to enable Pub-Sub. <br/>
    /// If assigned to <see cref="FlioxHub.EventDispatcher"/> the <see cref="FlioxHub"/> send
    /// push events to clients for database changes and messages these clients have subscribed. <br/>
    /// In case of remote database connections <b>WebSockets</b> are used to send push events to clients.   
    /// </summary>
    public sealed class EventDispatcher : IDisposable
    {
        private  readonly   SharedEnv                                       sharedEnv;
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        /// key: <see cref="EventSubClient.clientId"/>
        [DebuggerBrowsable(Never)] 
        private  readonly   ConcurrentDictionary<JsonKey, EventSubClient>   subClients;
        /// expose <see cref="subClients"/> as property to show them as list in Debugger
        // ReSharper disable once UnusedMember.Local
        private             ICollection<EventSubClient>                     SubClients => subClients.Values;
        
        private  readonly   ConcurrentDictionary<JsonKey, EventSubUser>     subUsers;

        internal readonly   bool                                            background;

        public   override   string                                          ToString() => $"subscribers: {subClients.Count}";

        private const string MissingEventReceiver = "subscribing events requires an eventReceiver. E.g a WebSocket as a target for push events.";

        public EventDispatcher (bool background, SharedEnv env = null) {
            sharedEnv       = env ?? SharedEnv.Default;
            jsonEvaluator   = new JsonEvaluator();
            subClients      = new ConcurrentDictionary<JsonKey, EventSubClient>(JsonKey.Equality);
            subUsers        = new ConcurrentDictionary<JsonKey, EventSubUser>(JsonKey.Equality);
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }
        
        internal bool TryGetSubscriber(JsonKey key, out EventSubClient subClient) {
            return subClients.TryGetValue(key, out subClient);
        }
        
        /// used for test assertion
        public int NotAcknowledgedEvents() {
            int count = 0;
            foreach (var pair in subClients) {
                count += pair.Value.SentEventsCount;
            }
            return count;
        }

        public async Task FinishQueues() {
            if (!background)
                return;
            var loopTasks = new List<Task>();
            foreach (var pair in subClients) {
                var subClient = pair.Value;
                subClient.FinishQueue();
                loopTasks.Add(subClient.triggerLoop);
            }
            await Task.WhenAll(loopTasks).ConfigureAwait(false);
        }
        
        // -------------------------------- add / remove subscriptions --------------------------------
        internal bool SubscribeMessage(
            string              database,
            SubscribeMessage    subscribe,
            User                user,
            in JsonKey          clientId,
            int                 eventAck,
            IEventReceiver      eventReceiver,
            out string          error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubClient subClient;
            var remove = subscribe.remove;
            if (remove.HasValue && remove.Value) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return true;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    return true;
                }
                databaseSubs.RemoveMessageSubscription(subscribe.name);
                RemoveEmptySubClient(subClient);
                return true;
            } else {
                subClient = GetOrCreateSubClient(user, clientId, eventAck, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database);
                    subClient.databaseSubs.Add(database, databaseSubs);
                }
                databaseSubs.AddMessageSubscription(subscribe.name);
                return true;
            }
        }

        internal bool SubscribeChanges (
            string              database,
            SubscribeChanges    subscribe,
            User                user,
            in JsonKey          clientId,
            int                 eventAck,
            IEventReceiver      eventReceiver,
            out string          error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubClient subClient;
            if (subscribe.changes.Count == 0) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return true;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                    return true;
                databaseSubs.RemoveChangeSubscription(subscribe.container);
                RemoveEmptySubClient(subClient);
                return true;
            } else {
                subClient = GetOrCreateSubClient(user, clientId, eventAck, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database);
                    subClient.databaseSubs.Add(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
                return true;
            }
        }
        
        private EventSubClient GetOrCreateSubClient(User user, in JsonKey clientId, int eventAck, IEventReceiver eventReceiver) {
            subClients.TryGetValue(clientId, out EventSubClient subClient);
            if (subClient != null)
                return subClient;
            if (!subUsers.TryGetValue(user.userId, out var subUser)) {
                subUser = new EventSubUser (user.userId, user.groups);
                subUsers.TryAdd(user.userId, subUser);
            }
            subClient = new EventSubClient(sharedEnv, subUser, clientId, eventAck, eventReceiver, background);
            subClients.TryAdd(clientId, subClient);
            subUser.clients.Add(subClient);
            return subClient;
        }
        
        private void RemoveEmptySubClient(EventSubClient subClient) {
            if (subClient.SubCount > 0)
                return;
            subClients.TryRemove(subClient.clientId, out _);
            var user = subClient.user;
            user.clients.Remove(subClient);
            if (user.clients.Count == 0) {
                subUsers.TryRemove(user.userId, out _);
            }
        }
        
        
        // -------------------------- event distribution --------------------------------
        // use only for testing
        internal async Task SendQueuedEvents() {
            if (background) {
                throw new InvalidOperationException("must not be called, if using a background Tasks");
            }
            foreach (var pair in subClients) {
                var subClient = pair.Value;
                await subClient.SendEvents().ConfigureAwait(false);
            }
        }
        
        private void ProcessSubscriber(SyncRequest syncRequest, SyncContext syncContext) {
            ref JsonKey  clientId = ref syncContext.clientId;
            if (clientId.IsNull())
                return;
            
            if (!subClients.TryGetValue(clientId, out var subClient))
                return;
            var eventReceiver = syncContext.eventReceiver;
            if (eventReceiver != null) {
                subClient.UpdateTarget (eventReceiver);
            }
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            int value =  eventAck.Value;
            subClient.AcknowledgeEvents(value);
        }
        
        internal void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext) {
            var database        = syncContext.DatabaseName;
            ProcessSubscriber (syncRequest, syncContext);
            
            using (var pooled = syncContext.ObjectMapper.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                writer.Pretty           = false;    // write sub's as one liner
                writer.WriteNullMembers = false;
                foreach (var pair in subClients) {
                    List<SyncRequestTask>  eventTasks = null;
                    EventSubClient     subClient = pair.Value;
                    if (subClient.SubCount == 0)
                        throw new InvalidOperationException("Expect SubscriptionCount > 0");
                    
                    if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                        continue;
                    
                    // Enqueue only change events for (change) tasks which are not send by the client itself
                    bool subscriberIsSender = syncContext.clientId.IsEqual(subClient.clientId);
                    databaseSubs.AddEventTasks(syncRequest, subClient, subscriberIsSender, ref eventTasks, jsonEvaluator);

                    if (eventTasks == null)
                        continue;
                    var eventMessage = new EventMessage {
                        tasks       = eventTasks.ToArray(),
                        srcUserId   = syncRequest.userId,
                        dstClientId = subClient.clientId
                    };
                    if (SerializeRemoteEvents && subClient.IsRemoteTarget) {
                        SerializeRemoteEvent(eventMessage, eventTasks, writer);
                    }
                    subClient.EnqueueEvent(eventMessage);
                }
            }
        }
        
        private static bool SerializeRemoteEvents = true; // set to false for development

        /// Optimization: For remote connections the tasks are serialized to <see cref="EventMessage.tasksJson"/>.
        /// Benefits of doing this:
        /// - serialize a task only once for multiple targets
        /// - storing only a single byte[] for a task instead of a complex SyncRequestTask which is not used anymore
        private static void SerializeRemoteEvent(EventMessage eventMessage, List<SyncRequestTask> tasks, ObjectWriter writer) {
            var tasksJson = new JsonValue [tasks.Count];
            eventMessage.tasksJson = tasksJson;
            for (int n = 0; n < tasks.Count; n++) {
                var task = tasks[n];
                if (task.json == null) {
                    task.json = new JsonValue(writer.WriteAsArray(task));
                }
                tasksJson[n] = task.json.Value;
            }
            tasks.Clear();
            eventMessage.tasks = null;
        }
    }
}