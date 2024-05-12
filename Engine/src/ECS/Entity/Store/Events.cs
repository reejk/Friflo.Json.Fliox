// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections.Generic;

// ReSharper disable UseCollectionExpression
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class EntityStore
{
    private static void RemoveAllEntityEventHandlers(EntityStore store, in EntityNode node)
    {
        var entityId    = node.id;
        var hasEvent    = node.hasEvent;
        RemoveAllEntityEventHandlers(store, entityId, hasEvent);
        if (node.signalTypeCount > 0) {
            var list = store.intern.signalHandlers;
            if (list != null) {
                foreach (var signalHandler in list) {
                    signalHandler.RemoveAllSignalHandlers(entityId);
                }
            }
        }
    }
    
#region subscribed event / signal delegates 
    internal static DebugEventHandlers GetEventHandlers(EntityStore store, int entityId)
    {
        List<DebugEventHandler> eventHandlers = null;
        var hasEvent = store.nodes[entityId].hasEvent;
        AddEventHandlers(ref eventHandlers, store, entityId, hasEvent);

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

