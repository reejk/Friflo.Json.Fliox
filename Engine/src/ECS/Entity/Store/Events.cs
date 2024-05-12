// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable UseCollectionExpression
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class EntityStore
{
#region add / remove script events
    private void ScriptChanged(ScriptChanged args)
    {
        if (!intern.entityScriptChanged.TryGetValue(args.Entity.Id, out var handlers)) {
            return;
        }
        handlers.Invoke(args);
    }
    
    internal static void AddScriptChangedHandler(EntityStore store, int entityId, Action<ScriptChanged> handler)
    {
        if (AddEntityHandler(store, entityId, handler, HasEventFlags.ScriptChanged ,ref store.intern.entityScriptChanged)) {
            store.intern.scriptAdded     += store.ScriptChanged;
            store.intern.scriptRemoved   += store.ScriptChanged;
        }
    }
    
    internal static void RemoveScriptChangedHandler(EntityStore store, int entityId, Action<ScriptChanged> handler)
    {
        if (RemoveEntityHandler(store, entityId, handler,  HasEventFlags.ScriptChanged, store.intern.entityScriptChanged)) {
            store.intern.scriptAdded     -= store.ScriptChanged;
            store.intern.scriptRemoved   -= store.ScriptChanged;
        }
    }
    #endregion
    
    private static void RemoveAllEntityEventHandlers(EntityStore store, in EntityNode node)
    {
        var entityId    = node.id;
        var hasEvent    = node.hasEvent;
        RemoveAllEntityEventHandlers(store, entityId, hasEvent);
        if ((hasEvent & HasEventFlags.ScriptChanged) != 0) {
            var handlerMap = store.intern.entityScriptChanged;
            handlerMap.Remove(entityId);
            if (handlerMap.Count == 0) {
                store.intern.scriptAdded            -= store.ScriptChanged;
                store.intern.scriptRemoved          -= store.ScriptChanged;
            }
        }
        if (node.signalTypeCount > 0) {
            var list = store.intern.signalHandlers;
            if (list != null) {
                foreach (var signalHandler in list) {
                    signalHandler.RemoveAllSignalHandlers(entityId);
                }
            }
        }
    }
    
    [ExcludeFromCodeCoverage]
    internal new static void AssertEventDelegatesNull(EntityStore store)
    {
        if (store.intern.scriptAdded            != null) throw new InvalidOperationException("expect null");
        if (store.intern.scriptRemoved          != null) throw new InvalidOperationException("expect null");
    }
    
#region subscribed event / signal delegates 
    internal static DebugEventHandlers GetEventHandlers(EntityStore store, int entityId)
    {
        List<DebugEventHandler> eventHandlers = null;
        var hasEvent = store.nodes[entityId].hasEvent;
        AddEventHandlers(ref eventHandlers, store, entityId, hasEvent);
        
        if ((hasEvent & HasEventFlags.ScriptChanged) != 0) {
            var handlers    = store.intern.entityScriptChanged[entityId];
            var handler     = new DebugEventHandler(DebugEntityEventKind.Event, typeof(ScriptChanged), handlers.GetInvocationList());
            eventHandlers ??= new List<DebugEventHandler>();
            eventHandlers.Add(handler);
        }
        var list = store.intern.signalHandlers;
        if (list != null) {
            if (store.nodes[entityId].signalTypeCount > 0) {
                foreach (var signalHandler in list) {
                    signalHandler.AddSignalHandler(entityId, ref eventHandlers);
                }
            }
        }
        return new DebugEventHandlers(eventHandlers);
    }
    #endregion
}

