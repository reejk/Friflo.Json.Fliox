// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ReturnTypeCanBeEnumerable.Global
namespace Friflo.Engine.ECS.Utils;

public static class TreeUtils
{
#region Duplicate Entity's
    /// <returns> the indexes of the duplicated entities within the parent of the original entities</returns>
    public static int[] DuplicateEntities(List<Entity> entities)
    {
        var indexes = new int [entities.Count];
        var store   = entities[0].Store;
        int pos     = 0;
        foreach (var entity in entities) {
            var parent  = entity.Parent;
            if (parent.IsNull) {
                indexes[pos++] = -1;
                continue;
            }
            var clone       = store.CloneEntity(entity);
            var index       = parent.AddChild(clone);
            indexes[pos++]  = index;
            DuplicateChildren(entity, clone, store);
        }
        return indexes;
    }
    
    private static void DuplicateChildren(Entity entity, Entity clone, EntityStore store)
    {
        foreach (var childId in entity.ChildIds) {
            var child       = store.GetEntityById(childId);
            var childClone  = store.CloneEntity(child);
            clone.AddChild(childClone);
            
            DuplicateChildren(child, childClone, store);
        }
    }
    
    #endregion
}