﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using static Friflo.Engine.ECS.NodeFlags;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable ConvertToAutoPropertyWhenPossible
// ReSharper disable ConvertToAutoProperty
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Used by the <see cref="EntityStore"/> to store <see cref="Entity"/> components, scripts, tags and child entities
/// internally as an array of nodes.
/// </summary> 
/// <remarks>
/// <see cref="EntityNode"/>'s enable organizing entities in a tree graph structure.<br/>
/// The tree graph is stored in an <see cref="EntityStore"/> starting with a single <see cref="EntityStore.StoreRoot"/> entity.<br/> 
/// <br/>
/// It provide the properties listed below
/// <list type="bullet">
///   <item><see cref="Id"/> to identify an entity in its <see cref="EntityStore"/></item>
///   <item><see cref="Pid"/> used to store entities in a database</item>
///   <item><see cref="Entity"/> to access the <see cref="ECS.Entity"/> attached to a node</item>
/// </list>
/// <b><see cref="Id"/></b><br/>
/// The entity id change when performing an <see cref="EntityStore"/> id cleanup.<br/>
/// The clean remove unused ids and ensure that entities with the same <see cref="Archetype"/> have consecutive ids.<br/>
/// <br/>
/// <b><see cref="Pid"/></b><br/>
/// When creating a new entity in the <see cref="EntityStore"/> it generates a random <see cref="Pid"/>
/// using <see cref="EntityStore.GenerateRandomPidForId"/>.<br/>
/// Using random pids avoid merge conflicts when multiples users make changes to the same <see cref="EntityStore"/> file / database.<br/>
/// The probability generating the same pid by two different users is:
/// <code>
///     p = 1 - exp(-r^2 / (2 * N))
///     r:  number of new entities added by a user to an existing <see cref="EntityStore"/> (not the number of all entities)
///     N:  number of possible values = int.MaxValue = 2147483647
/// </code>
/// See: https://en.wikipedia.org/wiki/Birthday_problem
/// </remarks>
public struct EntityNode
{
#region public properties
    /// <summary>The unique entity id.</summary>
                    public              int                 Id          =>  id;
    
    /// <summary>Permanent unique pid used for persistence of an entity in a database </summary>
                    public              long                Pid         =>  pid;
    
    /// <summary>The <see cref="ECS.Archetype"/> the entity node is stored.</summary>
                    public              Archetype           Archetype   =>  archetype;
          
    /// <summary>Internally used flags assigned to the entity.</summary>
                    public              NodeFlags           Flags       =>  flags;
                    
                    public   override   string              ToString()  => GetString();
    #endregion
    
#region internal fields
    [Browse(Never)] internal    int             id;             //  4   not readonly for perf. If readonly EnsureCapacity() & EnsureNodesLength() must call its constructor.  
    [Browse(Never)] internal    long            pid;            //  8   permanent id used for serialization
    /// <summary> Use <see cref="Is"/> or <see cref="IsNot"/> for read access. </summary>
    [Browse(Never)] internal    NodeFlags       flags;          //  4 (1)
    /// <remarks> Is set to null only in <see cref="EntityStore.DeleteNode"/>. </remarks>
    [Browse(Never)] internal    Archetype       archetype;      //  8   can be null. Could use int to relieve GC tracing reference types 
    [Browse(Never)] internal    int             compIndex;      //  4   index within Archetype.entityIds & StructHeap<>.components
    [Browse(Never)] internal    int             scriptIndex;    //  4   0 if entity has no scripts
    /// <remarks> Used to avoid enumeration of <see cref="EntityStore.Intern.signalHandlers"/> </remarks>
                    internal    byte            signalTypeCount;//  1   number of different signal types attached to the entity. 
                    internal    HasEventFlags   hasEvent;       //  1   bit is 1 in case an event handler is attached to the entity. 
    #endregion
    
#region internal getter
                    internal readonly   bool        Is      (NodeFlags flag) => (flags & flag) != 0;
                    internal readonly   bool        IsNot   (NodeFlags flag) => (flags & flag) == 0;
    #endregion
    
#region internal methods
    internal EntityNode(int id) {
        this.id = id;
    }
    
    private readonly string GetString()
    {
        var sb = new StringBuilder();
        if (archetype != null) {
            EntityUtils.EntityToString(id, archetype, sb);
        } else {
            sb.Append("id: ");
            sb.Append(id);
        }
        if (flags != 0) {
            sb.Append("  flags: ");
            var startPos = sb.Length;
            if (Is(Created)) {
                if (startPos < sb.Length) {
                    sb.Append(" | ");
                }
                sb.Append("Created");
            }
        }
        return sb.ToString();
    }
    #endregion
}

/// <summary>
/// Use to avoid Dictionary lookups for:
/// <see cref="EntityStoreBase.InternBase.entityComponentChanged"/><br/>
/// <see cref="EntityStoreBase.InternBase.entityTagsChanged"/><br/>
/// <see cref="EntityStore.Intern.entityScriptChanged"/><br/>
/// </summary>
[Flags]
internal enum HasEventFlags : byte
{
    /// <summary> Bit is set - <see cref="EntityStoreBase.InternBase.entityComponentChanged"/>.Count > 0<br/> </summary>
    ComponentChanged        = 1,
    /// <summary> Bit is set - <see cref="EntityStoreBase.InternBase.entityTagsChanged"/>.Count > 0<br/> </summary>
    TagsChanged             = 2,
    /// <summary> Bit is set - <see cref="EntityStore.Intern.entityScriptChanged"/>.Count > 0<br/> </summary>
    ScriptChanged           = 4,
}


