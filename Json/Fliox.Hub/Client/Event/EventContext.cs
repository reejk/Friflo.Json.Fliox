// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void SubscriptionHandler (EventContext context);
    
    public sealed class EventContext : ILogSource
    {
        public              JsonKey                         SrcUserId       => ev.srcUserId;
        public              int                             EventSequence   => processor.EventSequence;
        public              IReadOnlyList<Message>          Messages        => processor.messages;
        /// <summary> <see cref="Changes"/> return the changes per database <see cref="EntityChanges.Container"/>.
        /// Use <see cref="GetChanges{TKey,T}"/> to access specific container changes </summary>
        public              IReadOnlyList<EntityChanges>    Changes         => processor.contextChanges;
        public              EventInfo                       EventInfo       { get; private set; }
        
        [DebuggerBrowsable(Never)]
        public              IHubLogger                      Logger { get; }

        [DebuggerBrowsable(Never)]
        private readonly    SubscriptionProcessor           processor;
        
        [DebuggerBrowsable(Never)]
        private readonly    EventMessage                    ev;

        public  override    string                          ToString()  => $"source user: {ev.srcUserId}";

        internal EventContext(SubscriptionProcessor processor, EventMessage ev, IHubLogger logger) {
            this.processor  = processor;
            this.ev         = ev;
            EventInfo       = ev.GetEventInfo();
            Logger          = logger;
        }
        
        public EntityChanges<TKey, T> GetChanges<TKey, T>(EntitySet<TKey, T> entitySet) where T : class {
            return (EntityChanges<TKey, T>)processor.GetChanges(entitySet);
        } 
    }
}