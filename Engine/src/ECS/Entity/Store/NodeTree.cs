// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using static Friflo.Engine.ECS.NodeFlags;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class EntityStore
{
    // --------------------------------- tree node methods ---------------------------------
    /// <summary>
    /// Allocates memory for entities in the store to enable creating entities without reallocation.
    /// </summary>
    /// <returns>The number of entities that can be added without reallocation. </returns>
    public int EnsureCapacity(int capacity)
    {
        var curLength   = nodes.Length;
        var last        = intern.sequenceId + 1;
        var curCapacity = curLength - last;
        if (curCapacity >= capacity) {
            return curCapacity;
        }
        var newLength   = last + capacity;
        ArrayUtils.Resize(ref nodes, newLength);
        var localNodes = nodes;
        for (int n = curLength; n < newLength; n++) {
            localNodes[n].id = n;
            // localNodes[n] = new EntityNode (n);      // see: EntityNode.id comment
        }
        return newLength - last;
    }
    
    private void EnsureNodesLength(int length)
    {
        var curLength = nodes.Length;
        if (length <= curLength) {
            return;
        }
        var newLength = Math.Max(length, 2 * curLength); // could grow slower to minimize heap pressure
        ArrayUtils.Resize(ref nodes, newLength);
        var localNodes = nodes;
        for (int n = curLength; n < newLength; n++) {
            localNodes[n].id = n;
            // localNodes[n] = new EntityNode (n);      // see: EntityNode.id comment
        }
    }
    
    /// <summary>
    /// Set the seed used to create random entity <see cref="Entity.Pid"/>'s for an entity store <br/>
    /// created with <see cref="PidType"/> == <see cref="PidType.RandomPids"/>.
    /// </summary>
    public void SetRandomSeed(int seed) {
        intern.randPid = new Random(seed);
    }
    
    private long GeneratePid(int id) {
        return intern.pidType == PidType.UsePidAsId ? id : GenerateRandomPidForId(id);
    }
    
    private long GenerateRandomPidForId(int id)
    {
        while(true) {
            // generate random int to have numbers with small length e.g. 2147483647 (max int)
            // could also generate long which requires more memory when persisting entities
            long pid = intern.randPid.Next();
            if (intern.pid2Id.TryAdd(pid, id)) {
                return pid;
            }
        }
    }
    
    protected internal override void    UpdateEntityCompIndex(int id, int compIndex) {
        nodes[id].compIndex = compIndex;
    }
    
    internal int NewId()
    {
        var localNodes  = nodes;
        var max         = localNodes.Length;
        var id          = Interlocked.Increment(ref intern.sequenceId);
        for (; id < max;)
        {
            if ((localNodes[id].flags & Created) != 0) {
                id = Interlocked.Increment(ref intern.sequenceId);
                continue;
            }
            break;
        }
        return id;
    }
    
    /// <remarks> Set <see cref="EntityNode.archetype"/> = null. </remarks>
    internal void DeleteNode(int id)
    {
        entityCount--;
        var localNodes  = nodes;
        ref var node    = ref localNodes[id];
        
        RemoveAllEntityEventHandlers(this, node);
        // --- clear node entry.
        //     Set node.archetype = null
        node            = new EntityNode(id); // clear node
    }
}
