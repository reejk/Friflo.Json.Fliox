﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Database.Event
{
    public interface IEventTarget {
        bool        IsOpen ();
        Task<bool>  ProcessEvent(DatabaseEvent ev, SyncContext syncContext);
    }
    
    public class EventBroker : IDisposable
    {
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        /// key: <see cref="EventSubscriber.clientId"/>
        private  readonly   ConcurrentDictionary<string, EventSubscriber>   subscribers;
        internal readonly   bool                                            background;

        public EventBroker (bool background) {
            jsonEvaluator   = new JsonEvaluator();
            subscribers     = new ConcurrentDictionary<string, EventSubscriber>();
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }

        public async Task FinishQueues() {
            if (!background)
                return;
            var loopTasks = new List<Task>();
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                subscriber.FinishQueue();
                loopTasks.Add(subscriber.triggerLoop);
            }
            await Task.WhenAll(loopTasks).ConfigureAwait(false);
        }
        
        // -------------------------------- add / remove subscriptions --------------------------------
        internal void SubscribeEcho(SubscribeEcho subscribe, string clientId, IEventTarget eventTarget) {
            EventSubscriber subscriber;
            if (subscribe.prefixes.Count == 0) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return;
                subscriber.echoSubscriptions.Clear();
                RemoveOnEmptySubscriptions(subscriber, clientId);
                return;
            }
            subscriber = GetOrCreateSubscriber(clientId, eventTarget);
            subscriber.echoSubscriptions.Clear();
            foreach (var prefix in subscribe.prefixes) {
                subscriber.echoSubscriptions.Add(prefix);
            }
        }

        internal void SubscribeChanges (SubscribeChanges subscribe, string clientId, IEventTarget eventTarget) {
            EventSubscriber subscriber;
            if (subscribe.changes.Count == 0) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return;
                var subscriptions = subscriber.changeSubscriptions;
                subscriptions.Remove(subscribe.container);
                RemoveOnEmptySubscriptions(subscriber, clientId);
                return;
            }
            subscriber = GetOrCreateSubscriber(clientId, eventTarget);
            subscriber.changeSubscriptions[subscribe.container] = subscribe;
        }
        
        private EventSubscriber GetOrCreateSubscriber(string clientId, IEventTarget eventTarget) {
            subscribers.TryGetValue(clientId, out EventSubscriber subscriber);
            if (subscriber != null)
                return subscriber;
            subscriber = new EventSubscriber(clientId, eventTarget, background);
            subscribers.TryAdd(clientId, subscriber);
            return subscriber;
        }
        
        private void RemoveOnEmptySubscriptions(EventSubscriber subscriber, string clientId) {
            if (subscriber.SubscriptionCount > 0)
                return;
            subscribers.TryRemove(clientId, out _);
        }
        
        
        // -------------------------- event distribution --------------------------------
        // use only for testing
        internal async Task SendQueuedEvents() {
            if (background) {
                throw new InvalidOperationException("must not be called, if using a background Tasks");
            }
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                await subscriber.SendEvents().ConfigureAwait(false);
            }
        }
        
        private void ProcessSubscriber(SyncRequest syncRequest, SyncContext syncContext) {
            string  clientId = syncRequest.clientId;
            if (clientId == null)
                return;
            
            if (!subscribers.TryGetValue(clientId, out var subscriber))
                return;
            
            subscriber.UpdateTarget (syncContext.eventTarget);
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            int value =  eventAck.Value;
            subscriber.AcknowledgeEvents(value);
        }
        
        private static void AddTask(ref List<DatabaseTask> tasks, DatabaseTask task) {
            if (tasks == null) {
                tasks = new List<DatabaseTask>();
            }
            tasks.Add(task);
        }

        internal void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext) {
            ProcessSubscriber (syncRequest, syncContext);
            
            foreach (var pair in subscribers) {
                List<DatabaseTask>  tasks = null;
                EventSubscriber     subscriber = pair.Value;
                if (subscriber.SubscriptionCount == 0)
                    throw new InvalidOperationException("Expect SubscriptionCount > 0");
                
                // Enqueue only change events for (change) tasks which are not send by the client itself
                bool subscriberIsSender = syncRequest.clientId == subscriber.clientId;
                
                foreach (var task in syncRequest.tasks) {
                    foreach (var changesPair in subscriber.changeSubscriptions) {
                        if (subscriberIsSender)
                            continue;
                        SubscribeChanges subscribeChanges = changesPair.Value;
                        var taskResult = FilterTask(task, subscribeChanges);
                        if (taskResult == null)
                            continue;
                        AddTask(ref tasks, taskResult);
                    }
                    if (task.TaskType == TaskType.echo) {
                        var echo = (Echo) task;
                        foreach (var echoSubscription in subscriber.echoSubscriptions) {
                            if (!echo.message.StartsWith(echoSubscription))
                                continue;
                            AddTask(ref tasks, task);
                        }
                    }
                }
                if (tasks == null)
                    continue;
                var subscriptionEvent = new SubscriptionEvent {
                    tasks       = tasks,
                    clientId    = syncRequest.clientId,
                    targetId    = subscriber.clientId
                };
                subscriber.EnqueueEvent(subscriptionEvent);
            }
        }
        
        private DatabaseTask FilterTask (DatabaseTask task, SubscribeChanges subscribe) {
            switch (task.TaskType) {
                
                case TaskType.create:
                    if (!subscribe.changes.Contains(Change.create))
                        return null;
                    var create = (CreateEntities) task;
                    if (create.container != subscribe.container)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(subscribe.filter, create.entities)
                    };
                    return createResult;
                
                case TaskType.update:
                    if (!subscribe.changes.Contains(Change.update))
                        return null;
                    var update = (UpdateEntities) task;
                    if (update.container != subscribe.container)
                        return null;
                    var updateResult = new UpdateEntities {
                        container   = update.container,
                        entities    = FilterEntities(subscribe.filter, update.entities)
                    };
                    return updateResult;
                
                case TaskType.delete:
                    if (!subscribe.changes.Contains(Change.delete))
                        return null;
                    var delete = (DeleteEntities) task;
                    if (subscribe.container != delete.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.patch:
                    if (!subscribe.changes.Contains(Change.patch))
                        return null;
                    var patch = (PatchEntities) task;
                    if (subscribe.container != patch.container)
                        return null;
                    // todo apply filter
                    return task;
                
                default:
                    return null;
            }
        }
        
        private Dictionary<string, EntityValue> FilterEntities (FilterOperation filter, Dictionary<string, EntityValue> entities) {
            if (filter == null)
                return entities;
            var jsonFilter      = new JsonFilter(filter); // filter can be reused
            var result          = new Dictionary<string, EntityValue>();

            foreach (var entityPair in entities) {
                string      key     = entityPair.Key;
                EntityValue value   = entityPair.Value;
                var         payload = value.Json;
                if (jsonEvaluator.Filter(payload, jsonFilter)) {
                    result.Add(key, value);
                }
            }
            return result;
        }
    }
}