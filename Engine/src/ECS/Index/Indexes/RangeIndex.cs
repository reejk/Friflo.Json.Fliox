﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Index;

/// <summary>
/// A component index optimized to execute range queries in O(log N) at the cost of index updates in O(log N).<br/>
/// The default index executes in O(1) when adding, removing or updating indexed component values. 
/// </summary>
[ExcludeFromCodeCoverage] // not used - kept only for reference
public sealed class RangeIndex<TIndexedComponent,TValue> : ComponentIndex<TValue>
    where TIndexedComponent : struct, IIndexedComponent<TValue>
{
    internal override   int                         Count       => map.Count;
    
#region fields
    /// map: indexed value -> entity ids
    private  readonly   SortedList<TValue, IdArray> map         = new();
    
    private             ReadOnlyCollection<TValue>  keyCollection;
    #endregion
    
#region indexing
    internal override void Add<TComponent>(int id, in TComponent component)
    {
        var value = IndexUtils<TComponent,TValue>.GetIndexedValue(component);
    //  var value = ((IIndexedComponent<TValue>)component).GetIndexedValue();    // boxes component
        SortedListUtils.AddComponentValue    (id, value, map, this);
    }
    
    internal override void Update<TComponent>(int id, in TComponent component, StructHeap<TComponent> heap)
    {
        var oldValue = IndexUtils<TComponent,TValue>.GetIndexedValue(heap.componentStash);
        var value    = IndexUtils<TComponent,TValue>.GetIndexedValue(component);
        if (EqualityComparer<TValue>.Default.Equals(oldValue , value)) {
            return;
        }
        var localMap  = map;
        SortedListUtils.RemoveComponentValue (id, oldValue, localMap, this);
        SortedListUtils.AddComponentValue    (id, value,    localMap, this);
    }

    internal override void Remove<TComponent>(int id, StructHeap<TComponent> heap)
    {
        var value = IndexUtils<TComponent,TValue>.GetIndexedValue(heap.componentStash);
        SortedListUtils.RemoveComponentValue (id, value, map, this);
    }
    
    internal override void RemoveEntityIndex(int id, Archetype archetype, int compIndex)
    {
        var localMap    = map;
        var heap        = idHeap;
        var components  = ((StructHeap<TIndexedComponent>)archetype.heapMap[componentType.StructIndex]).components;
        var value       = components[compIndex].GetIndexedValue();
        localMap.TryGetValue(value, out var idArray);
        var idSpan  = idArray.GetIdSpan(heap);
        var index   = idSpan.IndexOf(id);
        idArray.RemoveAt(index, heap);
        if (idArray.Count == 0) {
            localMap.Remove(value);
        } else {
            localMap[value] = idArray;
        }
        store.nodes[id].references &= ~indexBit;
    }
    #endregion
    
#region get matches
    internal override Entities GetHasValueEntities(TValue value)
    {
        map.TryGetValue(value, out var ids);
        return idHeap.GetEntities(store, ids);
    }
    
    internal override void AddValueInRangeEntities(TValue min, TValue max, HashSet<int> idSet)
    {
        var keys        = map.Keys;
        var heap        = idHeap;
        var localStore  = store;
        int lowerIndex  = RangeUtils<TValue>.LowerBound(keys, min);
        int upperIndex  = RangeUtils<TValue>.UpperBound(keys, max);
        
        var values = map.Values;
        for (int n = lowerIndex; n < upperIndex; n++) {
            var idArray = values[n];
            var entities = heap.GetEntities(localStore, idArray);
            foreach (var id in entities.Ids) {
                idSet.Add(id);
            }
        }
    }
    
    internal override IReadOnlyCollection<TValue> IndexedComponentValues => keyCollection ??= new ReadOnlyCollection<TValue>(map.Keys);
    #endregion
}

[ExcludeFromCodeCoverage] // not used - kept only for reference
internal static class RangeUtils<TValue>
{
    private static readonly Comparer<TValue> Comparer = Comparer<TValue>.Default; 
        
    // https://stackoverflow.com/questions/23806296/what-is-the-fastest-way-to-get-all-the-keys-between-2-keys-in-a-sortedlist
    internal static int LowerBound(IList<TValue> list, TValue value)
    {
        int lower = 0, upper = list.Count - 1;

        while (lower <= upper)
        {
            int middle = lower + (upper - lower) / 2;
            int comparisonResult = Comparer.Compare(value, list[middle]);

            // slightly adapted here
            if (comparisonResult <= 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }
        return lower;
    }
    
    internal static int UpperBound(IList<TValue> list, TValue value)
    {
        int lower = 0, upper = list.Count - 1;

        while (lower <= upper)
        {
            int middle = lower + (upper - lower) / 2;
            int comparisonResult = Comparer.Compare(value, list[middle]);

            // slightly adapted here
            if (comparisonResult < 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }
        return lower;
    }
}