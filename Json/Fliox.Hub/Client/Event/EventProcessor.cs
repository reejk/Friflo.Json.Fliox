// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Threading;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    public interface IEventProcessor
    {
        void EnqueueEvent(FlioxClient client, EventMessage ev);
    }
    
    public sealed class DirectEventProcessor : IEventProcessor
    {
        public void EnqueueEvent(FlioxClient client, EventMessage ev) {
            client._intern.SubscriptionProcessor.ProcessEvent(client, ev);
        }
    }
    
    /// <summary>
    /// Creates a <see cref="IEventProcessor"/> using a <see cref="SynchronizationContext"/>.<br/>
    /// This ensures that the handler methods passed to the <b>Subscribe*()</b> methods of
    /// <see cref="FlioxClient"/> and <see cref="EntitySet{TKey,T}"/> are called on the on the thread associated with
    /// the <see cref="SynchronizationContext"/>
    /// <br/>
    /// Depending on the application type <b>SynchronizationContext.Current</b> is null or not-null
    /// <list type="bullet">
    ///   <item>
    ///     In case of UI applications like WinForms, WPF or Unity <see cref="SynchronizationContext.Current"/> is not-null.<br/>
    ///     These application types utilize <see cref="SynchronizationContext.Current"/> to enables calling all UI methods
    ///     on the UI thread.
    ///   </item> 
    ///   <item>
    ///     In case of unit-tests or Console / ASP.NET Core applications <see cref="SynchronizationContext.Current"/> is null.<br/>
    ///     One way to establish a <see cref="SynchronizationContext"/> in these scenarios is to execute the whole
    ///     application / unit test within the <b>Run()</b> method of <see cref="SingleThreadSynchronizationContext"/>
    ///   </item>
    /// </list>
    /// </summary>
    public sealed class SynchronizationContextProcessor : IEventProcessor
    {
        private  readonly   SynchronizationContext  synchronizationContext;
        

        public SynchronizationContextProcessor(SynchronizationContext synchronizationContext) {
            this.synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
        }
        
        public SynchronizationContextProcessor() {
            synchronizationContext =
                SynchronizationContext.Current
                ?? throw new InvalidOperationException(SynchronizationContextIsNull);
        }
        
        private const string SynchronizationContextIsNull = @"SynchronizationContext.Current is null.
This is typically the case in console applications or unit tests. 
Consider running application / test withing SingleThreadSynchronizationContext.Run()";
        
        public void EnqueueEvent(FlioxClient client, EventMessage ev) {
            synchronizationContext.Post(delegate {
                client._intern.SubscriptionProcessor.ProcessEvent(client, ev);
            }, null);
        }
    }
    
    public sealed class QueuingEventProcessor : IEventProcessor
    {
        private readonly    ConcurrentQueue <QueuedMessage>      eventQueue = new ConcurrentQueue <QueuedMessage> ();

        /// <summary>
        /// Creates a queuing <see cref="IEventProcessor"/>.
        /// In this case the application must frequently call <see cref="ProcessEvents"/> to apply changes to the
        /// <see cref="FlioxClient"/>.
        /// This allows to specify the exact code point in an application (e.g. Unity) where <see cref="EventMessage"/>'s
        /// are applied to the <see cref="FlioxClient"/>.
        /// </summary>
        public QueuingEventProcessor() { }
        
        public void EnqueueEvent(FlioxClient client, EventMessage ev) {
            eventQueue.Enqueue(new QueuedMessage(client, ev));
        }
        
        /// <summary>
        /// Need to be called frequently by application to process subscription events.
        /// </summary>
        public void ProcessEvents() {
            while (eventQueue.TryDequeue(out QueuedMessage queuedMessage)) {
                var client  = queuedMessage.client;
                client._intern.SubscriptionProcessor.ProcessEvent(client, queuedMessage.ev);
            }
        }

        private readonly struct QueuedMessage
        {
            internal  readonly  FlioxClient     client;
            internal  readonly  EventMessage    ev;
            
            internal QueuedMessage(FlioxClient client, EventMessage  ev) {
                this.client = client;
                this.ev     = ev;
            }
        }
    }
}