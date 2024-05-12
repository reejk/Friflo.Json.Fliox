// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Used to provide additional debug information for an <see cref="Entity"/>:<br/>
/// <see cref="Entity.Pid"/>                <br/>
/// <see cref="Entity.Enabled"/>            <br/>
/// <see cref="Entity.DebugEventHandlers"/> <br/>
/// </summary>
internal readonly struct EntityInfo
{
#region properties
    internal            long                Pid             => entity.Pid;
    internal            bool                Enabled         => entity.Enabled;
    internal            DebugEventHandlers  EventHandlers   => EntityStore.GetEventHandlers(entity.store, entity.Id);
    public   override   string              ToString()      => "";
    #endregion

    [Browse(Never)] private readonly Entity entity;
    
    internal EntityInfo(Entity entity) {
        this.entity = entity;
    }
}

/// <summary>
/// Used to check if two <see cref="Entity"/> instances are the same entity by comparing their <see cref="Entity.Id"/>'s. 
/// </summary>
public sealed class EntityEqualityComparer : IEqualityComparer<Entity>
{
    public  bool    Equals(Entity left, Entity right)   => left.Id == right.Id;
    public  int     GetHashCode(Entity entity)          => entity.Id;
}


public static class EntityUtils
{
    public static  readonly EntityEqualityComparer EqualityComparer = new ();
 
    // ------------------------------------------- public methods -------------------------------------------
#region non generic component - methods
    /// <summary>
    /// Returns a copy of the entity component as an object.<br/>
    /// The returned <see cref="IComponent"/> is a boxed struct.<br/>
    /// So avoid using this method whenever possible. Use <see cref="Entity.GetComponent{T}"/> instead.
    /// </summary>
    public static  IComponent   GetEntityComponent    (Entity entity, ComponentType componentType) {
        return entity.archetype.heapMap[componentType.StructIndex].GetComponentDebug(entity.compIndex);
    }

    public static  bool RemoveEntityComponent (Entity entity, ComponentType componentType)
    {
        return componentType.RemoveEntityComponent(entity);
    }
    
    public static  bool AddEntityComponent    (Entity entity, ComponentType componentType) {
        return componentType.AddEntityComponent(entity);
    }
    
    public static  bool AddEntityComponentValue(Entity entity, ComponentType componentType, object value) {
        return componentType.AddEntityComponentValue(entity, value);
    }
    #endregion
    
    // ------------------------------------------- internal methods -------------------------------------------
#region internal - methods
    internal static int ComponentCount (this Entity entity) {
        return entity.archetype.componentCount;
    }
    
    internal static Exception NotImplemented(int id, string use) {
        var msg = $"to avoid excessive boxing. Use {use} or {nameof(EntityUtils)}.{nameof(EqualityComparer)}. id: {id}";
        return new NotImplementedException(msg);
    }
    
    internal static string EntityToString(Entity entity) {
        if (entity.store == null) {
            return "null";
        }
        return EntityToString(entity.Id, entity.archetype, new StringBuilder());
    }
    
    internal static string EntityToString(int id, Archetype archetype, StringBuilder sb)
    {
        sb.Append("id: ");
        sb.Append(id);
        if (archetype == null) {
            sb.Append("  (detached)");
            return sb.ToString();
        }
        var entity = new Entity(archetype.entityStore, id);
        var typeCount = archetype.componentCount + archetype.tags.Count; 
        if (typeCount == 0) {
            sb.Append("  []");
        } else {
            sb.Append("  [");
            foreach (var heap in archetype.Heaps()) {
                sb.Append(heap.StructType.Name);
                sb.Append(", ");
            }
            foreach (var tag in archetype.Tags) {
                sb.Append('#');
                sb.Append(tag.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(']');
        }
        return sb.ToString();
    }
    
    // ---------------------------------- Utils ----------------------------------
    internal static readonly Tags                           Disabled            = Tags.Get<Disabled>();
    
    #endregion
}