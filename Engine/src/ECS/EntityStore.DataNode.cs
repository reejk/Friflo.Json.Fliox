﻿using System;
using System.Collections.Generic;
using Fliox.Engine.Client;
using static Fliox.Engine.ECS.StoreOwnership;

namespace Fliox.Engine.ECS;

// This file contains implementation specific for storing DataNode's.
public sealed partial class EntityStore
{
/// <returns>an <see cref="attached"/> entity</returns>
    public GameEntity CreateFromDataNode(DataNode dataNode)
    {
        if (dataNode == null) {
            throw new ArgumentNullException(nameof(dataNode));
        }
        if (pidType == PidType.UsePidAsId) {
            return CreateFromDataNodeUsePidAsId(dataNode);
        }
        // --- map pid to id
        var pid     = dataNode.pid;
        var pidMap  = pid2Id;
        if (!pidMap.TryGetValue(pid, out int id)) {
            id = sequenceId++;
            pidMap.Add(pid, id);
        }
        // --- map children pid's to id's
        int[] childIds  = null;
        var children    = dataNode.children;
        var childCount  = 0;
        if (children != null) {
            childCount = children.Count;
            childIds = new int[childCount];
            for (int n = 0; n < childCount; n++) {
                var childPid = children[n];
                if (!pidMap.TryGetValue(childPid, out int childId)) {
                    childId = sequenceId++;
                    pidMap.Add(childPid, childId);
                }
                childIds[n] = childId;
            }
        }
        EnsureNodesLength(sequenceId);
        var entity  = CreateEntityInternal(id, pid);
        
        if (childIds != null) {
            UpdateEntityNodes(childIds, children);
            SetChildNodes(id, childIds, childCount);
        }
        return entity;
    }
    
    private GameEntity CreateFromDataNodeUsePidAsId(DataNode dataNode) {
        var id          = dataNode.pid;
        // --- use pid's as id's
        int[] childIds  = null;
        var children    = dataNode.children;
        var maxPid      = id;
        var childCount  = 0;
        if (children != null) {
            childIds    = children.ToArray();
            childCount  = children.Count; 
            foreach (var childId in childIds) {
                maxPid = Math.Max(maxPid, childId);
            }
        }
        EnsureNodesLength(maxPid + 1);
        var entity  = CreateEntityInternal(id, id);
        
        if (childIds != null) {
            UpdateEntityNodes(childIds, children);
            SetChildNodes(id, childIds, childCount);
        }
        return entity;
    }
    
    /// update EntityNode.pid of the child nodes
    private void UpdateEntityNodes(int[] childIds, List<int> children)
    {
        for (int n = 0; n < childIds.Length; n++) {
            var childId         = childIds[n];
            nodes[childId].pid  = children[n];
        }
    }
}
