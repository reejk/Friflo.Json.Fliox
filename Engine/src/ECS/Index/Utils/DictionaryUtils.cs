﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Index;

internal static class DictionaryUtils
{
    internal static void RemoveComponentValue<TValue>(int id, in TValue value, Dictionary<TValue, IdArray> map, ComponentIndex componentIndex)
    {
        var idHeap  = componentIndex.idHeap;
        map.TryGetValue(value, out var ids);
        var idSpan  = ids.GetIdSpan(idHeap);
        var index   = idSpan.IndexOf(id);
        if (index == -1) {
            return; // unexpected. Better safe than sorry. Used belts with suspenders :)
        }
        componentIndex.store.nodes[id].references &= ~componentIndex.indexBit;
        if (ids.Count == 1) {
            componentIndex.modified = true; 
            map.Remove(value);
            return;
        }
        ids.RemoveAt(index, idHeap);
        map[value] = ids;
    }
    
    internal static void AddComponentValue<TValue>(int id, in TValue value, Dictionary<TValue, IdArray> map, ComponentIndex componentIndex)
    {
        var idHeap = componentIndex.idHeap;
        map.TryGetValue(value, out var ids);
        var idSpan = ids.GetIdSpan(idHeap);
        if (idSpan.IndexOf(id) != -1) {
            return; // unexpected. Better safe than sorry. Used belts with suspenders :)
        }
        componentIndex.store.nodes[id].references |= componentIndex.indexBit;
        if (ids.Count == 0) {
            componentIndex.modified = true;
        }
        ids.AddId(id, idHeap);
        map[value] = ids;
    }
}