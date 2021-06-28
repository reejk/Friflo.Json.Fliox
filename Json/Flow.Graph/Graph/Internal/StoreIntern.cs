﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal struct StoreIntern
    {
        internal readonly   string                                  clientId;
        internal readonly   TypeStore                               typeStore;
        internal readonly   TypeStore                               ownedTypeStore;
        internal readonly   TypeCache                               typeCache;
        internal readonly   ObjectMapper                            jsonMapper;

        internal readonly   ObjectPatcher                           objectPatcher;
        
        internal readonly   EntityDatabase                          database;
        internal readonly   Dictionary<Type,   EntitySet>           setByType;
        internal readonly   Dictionary<string, EntitySet>           setByName;
        internal readonly   Pools                                   contextPools;
        internal readonly   EventTarget                             eventTarget;
        internal readonly   Dictionary<string, MessageSubscriber>   subscriptions;
        internal readonly   ObjectReader                            messageReader;
        
        // --- non readonly
        internal            SyncStore                               sync;
        internal            LogTask                                 tracerLogTask;
        internal            SubscriptionHandler                     subscriptionHandler;
        internal            bool                                    disposed;
        internal            int                                     lastEventSeq;
        internal            int                                     syncCount;
        

        public   override   string                                  ToString() => clientId;


        internal StoreIntern(
            string                  clientId,
            TypeStore               typeStore,
            TypeStore               owned,
            EntityDatabase          database,
            ObjectMapper            jsonMapper,
            EventTarget             eventTarget,
            SyncStore               sync)
        {
            this.clientId               = clientId;
            this.typeStore              = typeStore;
            this.ownedTypeStore         = owned;
            this.database               = database;
            this.jsonMapper             = jsonMapper;
            this.typeCache              = jsonMapper.writer.TypeCache;
            this.eventTarget            = eventTarget;
            this.sync                   = sync;
            setByType                   = new Dictionary<Type, EntitySet>();
            setByName                   = new Dictionary<string, EntitySet>();
            objectPatcher               = new ObjectPatcher(jsonMapper);
            contextPools                = new Pools(Pools.SharedPools);
            subscriptions               = new Dictionary<string, MessageSubscriber>();
            messageReader               = new ObjectReader(typeStore);
            tracerLogTask               = null;
            subscriptionHandler         = null;
            lastEventSeq                = 0;
            disposed                    = false;
            syncCount                   = 0;
        }
        
        internal Dictionary<string, MessageSubscriber> AddMessageHandler<TMessage> (string tag, Action<TMessage> action) {
            if (!subscriptions.TryGetValue(tag, out var subscriber)) {
                subscriber = new MessageSubscriber();
                subscriptions.Add(tag, subscriber);
            }
            if (action == null)
                return subscriptions;
            var messageHandler = new MessageHandler<TMessage>(action);
            subscriber.handlers.Add(messageHandler);
            return subscriptions;
        }
        
        internal Dictionary<string, MessageSubscriber> RemoveMessageHandler<TMessage> (string tag, Action<TMessage> action) {
            if (!subscriptions.TryGetValue(tag, out var subscriber)) {
                return subscriptions;
            }
            foreach (var handler in subscriber.handlers) {
                if (handler.HasAction(action))
                    subscriber.handlers.Remove(handler);
            }
            if (subscriber.handlers.Count == 0) {
                subscriptions.Remove(tag);
            }
            return subscriptions;
        }
    }
}
