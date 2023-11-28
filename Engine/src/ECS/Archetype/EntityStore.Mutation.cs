﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using static Friflo.Fliox.Engine.ECS.StructInfo;

// Hard rule: this file MUST NOT use type: Entity

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertConstructorToMemberInitializers
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

public partial class EntityStoreBase
{
#region get / add archetype
    internal bool TryGetValue(ArchetypeKey searchKey, out ArchetypeKey archetypeKey) {
        return archSet.TryGetValue(searchKey, out archetypeKey);
    }
        
    private Archetype GetArchetypeWith(Archetype current, int structIndex)
    {
        searchKey.SetWith(current, structIndex);
        if (archSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config          = GetArchetypeConfig();
        var schema          = Static.EntitySchema;
        var heaps           = current.Heaps;
        var componentTypes  = new List<ComponentType>(heaps.Length + 1);
        foreach (var heap in current.Heaps) {
            componentTypes.Add(schema.components[heap.structIndex]);
        }
        componentTypes.Add(schema.components[structIndex]);
        var archetype = Archetype.CreateWithStructTypes(config, componentTypes, current.tags);
        AddArchetype(archetype);
        return archetype;
    }
    
    private Archetype GetArchetypeWithout(Archetype archetype, int structIndex)
    {
        searchKey.SetWithout(archetype, structIndex);
        if (archSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var heaps           = archetype.Heaps;
        var componentCount  = heaps.Length - 1;
        var componentTypes  = new List<ComponentType>(componentCount);
        var config          = GetArchetypeConfig();
        var schema          = Static.EntitySchema;
        foreach (var heap in heaps) {
            if (heap.structIndex == structIndex)
                continue;
            componentTypes.Add(schema.components[heap.structIndex]);
        }
        var result = Archetype.CreateWithStructTypes(config, componentTypes, archetype.tags);
        AddArchetype(result);
        return result;
    }
    
    private Archetype GetArchetypeWithTags(Archetype archetype, in Tags tags)
    {
        var heaps           = archetype.Heaps;
        var componentTypes  = new List<ComponentType>(heaps.Length);
        var config          = GetArchetypeConfig();
        var schema          = Static.EntitySchema;
        foreach (var heap in heaps) {
            componentTypes.Add(schema.components[heap.structIndex]);
        }
        var result = Archetype.CreateWithStructTypes(config, componentTypes, tags);
        AddArchetype(result);
        return result;
    }
    
    internal void AddArchetype (Archetype archetype)
    {
        if (archsCount == archs.Length) {
            var newLen = 2 * archs.Length;
            Utils.Resize(ref archs,     newLen);
        }
        if (archetype.archIndex != archsCount) {
            throw new InvalidOperationException($"invalid archIndex. expect: {archsCount}, was: {archetype.archIndex}");
        }
        archs[archsCount] = archetype;
        archsCount++;
        archSet.Add(archetype.key);
    }
    #endregion
    
    // ------------------------------------ add / remove component ------------------------------------
#region add / remove component
    /// <remarks>
    /// Minimize method body size requiring generic type <typeparam name="T"></typeparam> by
    /// extracting <see cref="AddComponentInternal"/>.<br/>
    /// This minimizes the code generated by the runtime for each specific generic type.
    /// </remarks>
    internal bool AddComponent<T>(
            int         id,
            int         structIndex,
        ref Archetype   archetype,  // possible mutation is not null
        ref int         compIndex,
        in  T           component)
        where T : struct, IComponent
    {
        var result  = AddComponentInternal(id, ref archetype, ref compIndex, structIndex, out var structHeap);
        // --- change component value 
        var heap    = (StructHeap<T>)structHeap;
        heap.chunks[compIndex / ChunkSize].components[compIndex % ChunkSize] = component;
        return result;
    }
    
    private bool AddComponentInternal(
        int             id,
        ref Archetype   archetype,  // possible mutation is not null
        ref int         compIndex,
        int             structIndex,
        out StructHeap  structHeap)
    {
        var arch        = archetype;

        if (arch != defaultArchetype) {
            structHeap = arch.heapMap[structIndex];
            if (structHeap != null) {
                // case: archetype contains the component type
                return false;
            }
            // --- change entity archetype
            var newArchetype    = GetArchetypeWith(arch, structIndex);
            compIndex           = arch.MoveEntityTo(id, compIndex, newArchetype);
            archetype           = arch = newArchetype;
        } else {
            // --- add entity to archetype
            arch                = GetArchetype(arch.tags, structIndex);
            compIndex           = arch.AddEntity(id);
            archetype           = arch;
        }
        // --- set component value
        structHeap = arch.heapMap[structIndex];
        return true;
    }
    
    internal bool RemoveComponent(
            int         id,
        ref Archetype   archetype,    // possible mutation is not null
        ref int         compIndex,
            int         structIndex)
    {
        var arch    = archetype;
        var heap    = arch.heapMap[structIndex];
        if (heap == null) {
            return false;
        }
        var newArchetype = GetArchetypeWithout(arch, structIndex);
        if (newArchetype == defaultArchetype) {
            int removePos = compIndex; 
            // --- update entity
            archetype   = defaultArchetype;
            compIndex   = 0;
            arch.MoveLastComponentsTo(removePos);
            return true;
        }
        // --- change entity archetype
        archetype   = newArchetype;
        compIndex   = arch.MoveEntityTo(id, compIndex, newArchetype);
        return true;
    }
    #endregion
    
    // ------------------------------------ add / remove entity Tag ------------------------------------
#region add / remove tags

    internal bool AddTags(
        in Tags             tags,
        int                 id,
        ref Archetype       archetype,      // possible mutation is not null
        ref int             compIndex)
    {
        var arch            = archetype;
        var archTagsValue   = arch.tags.bitSet.value;
        var tagsValue       = tags.bitSet.value;
        if (archTagsValue == tagsValue) {
            return false;
        } 
        searchKey.structs           = arch.structs;
        searchKey.tags.bitSet.value = archTagsValue | tagsValue;
        searchKey.CalculateHashCode();
        Archetype newArchetype;
        if (archSet.TryGetValue(searchKey, out var archetypeKey)) {
            newArchetype = archetypeKey.archetype;
        } else {
            newArchetype = GetArchetypeWithTags(arch, searchKey.tags);
        }
        if (arch != defaultArchetype) {
            archetype   = newArchetype;
            compIndex   = arch.MoveEntityTo(id, compIndex, newArchetype);
            return true;
        }
        compIndex           = newArchetype.AddEntity(id);
        archetype           = newArchetype;
        return true;
    }
    
    internal bool RemoveTags(
        in Tags             tags,
        int                 id,
        ref Archetype       archetype,      // possible mutation is not null
        ref int             compIndex)
    {
        var arch            = archetype;
        var archTags        = arch.tags.bitSet.value;
        var archTagsRemoved = archTags & ~tags.bitSet.value;
        if (archTagsRemoved == archTags) {
            return false;
        }
        searchKey.structs           = arch.structs;
        searchKey.tags.bitSet.value = archTagsRemoved;
        searchKey.CalculateHashCode();
        Archetype newArchetype;
        if (archSet.TryGetValue(searchKey, out var archetypeKey)) {
            newArchetype = archetypeKey.archetype;
        } else {
            newArchetype = GetArchetypeWithTags(arch, searchKey.tags);
        }
        if (newArchetype == defaultArchetype) {
            int removePos = compIndex; 
            // --- update entity
            compIndex   = 0;
            archetype   = defaultArchetype;
            arch.MoveLastComponentsTo(removePos);
            return true;
        }
        compIndex   = arch.MoveEntityTo(id, compIndex, newArchetype);
        archetype   = newArchetype;
        return true;
    }
    #endregion
}
