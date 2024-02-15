﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


// ReSharper disable once CheckNamespace
// ReSharper disable UseNullPropagation
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace Friflo.Engine.ECS;

public partial class EntityStoreBase
{
    internal EntityBatch GetBatch(int entityId)
    {
        var batch               = internBase.batch ??= new EntityBatch(this);
        batch.entityId          = entityId;
        batch.addTags           = default;
        batch.removeTags        = default;
        batch.addComponents     = default;
        batch.removeComponents  = default;
        return batch;
    }
    
    internal void ApplyEntityBatch(EntityBatch batch)
    {
        var entityId    = batch.entityId;
        ref var node    = ref batch.entityStore.nodes[entityId];
        var archetype   = node.archetype;
        var compIndex   = node.compIndex;
        
        // --- apply AddTag() / RemoveTag() commands
        var newTags     = archetype.tags;
        newTags.Add    (batch.addTags);
        newTags.Remove (batch.removeTags);
        
        // --- apply AddComponent() / RemoveComponent() commands
        var newComponentTypes = archetype.componentTypes;
        newComponentTypes.Add   (batch.addComponents);
        newComponentTypes.Remove(batch.removeComponents);
        
        // --- change archetype
        var newArchetype = GetArchetype(newComponentTypes, newTags);
        if (newArchetype != archetype) {
            node.compIndex  = compIndex = Archetype.MoveEntityTo(archetype, entityId, compIndex, newArchetype);
            node.archetype  = newArchetype;
        }
        
        // --- assign AddComponent() values
        var newHeapMap  = newArchetype.heapMap;
        var components  = batch.components;
        foreach (var componentType in batch.addComponents) {
            newHeapMap[componentType.StructIndex].SetBatchComponent(components, compIndex);
        }
        
        // ----------- Send events for all batch commands. See: SEND_EVENT notes
        // --- send tags changed event
        var tagsChanged = internBase.tagsChanged;
        if (tagsChanged != null) {
            if (!newTags.bitSet.Equals(archetype.tags.bitSet)) {
                tagsChanged.Invoke(new TagsChanged(this, entityId, newTags, archetype.tags));
            }
        }
        // --- send component removed event
        if (internBase.componentRemoved != null) {
            SendComponentRemoved(batch, archetype, entityId);
        }
        // --- send component added event
        if (internBase.componentAdded != null) {
            SendComponentAdded(batch, archetype, compIndex);
        }
    }
    
    private void SendComponentAdded(EntityBatch batch, Archetype archetype, int compIndex)
    {
        var oldHeapMap      = archetype.heapMap;
        var componentAdded  = internBase.componentAdded;
        foreach (var componentType in batch.addComponents)
        {
            var structIndex = componentType.StructIndex;
            var structHeap  = oldHeapMap[structIndex];
            ComponentChangedAction action;
            if (structHeap == null) {
                action = ComponentChangedAction.Add;
            } else {
                // --- case: archetype contains the component type  => archetype remains unchanged
                structHeap.StashComponent(compIndex);
                action = ComponentChangedAction.Update;
            }
            componentAdded.Invoke(new ComponentChanged (this, batch.entityId, action, structIndex, structHeap));
        }
    }
    
    private void SendComponentRemoved(EntityBatch batch, Archetype archetype, int compIndex)
    {
        var oldHeapMap          = archetype.heapMap;
        var componentRemoved    = internBase.componentRemoved;
        foreach (var componentType in batch.removeComponents)
        {
            var structIndex = componentType.StructIndex;
            var oldHeap     = oldHeapMap[structIndex];
            if (oldHeap == null) {
                continue;
            }
            oldHeap.StashComponent(compIndex);
            componentRemoved.Invoke(new ComponentChanged (this, batch.entityId, ComponentChangedAction.Remove, structIndex, oldHeap));
        }
    }
}