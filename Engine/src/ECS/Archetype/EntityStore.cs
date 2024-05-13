﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// Hard rule: this file MUST NOT use type: Entity

// ReSharper disable once CheckNamespace
// ReSharper disable ConvertToAutoPropertyWhenPossible
namespace Friflo.Engine.ECS;

/// <summary>
/// Store the <see cref="IComponent"/>s and <see cref="ITag"/> for the <see cref="Entity"/>'s of an <see cref="EntityStore"/>.  
/// </summary>
/// <remarks>
/// <see cref="EntityStoreBase"/> is separated as a base from <see cref="EntityStore"/> as is can be used for<br/>
/// different entity store implementations like the <see cref="RawEntityStore"/>.
/// </remarks>
[CLSCompliant(true)]
public abstract partial class EntityStoreBase
{
#region public properties
    /// <summary>Number of all entities stored in the entity store</summary>
    [Browse(Never)] public              int                     Count           => entityCount;
    
    /// <summary> Obsolete. Renamed to <see cref="Count"/>. </summary>
    [Obsolete($"Renamed to {nameof(Count)}")]
    [Browse(Never)] public              int                     EntityCount     => entityCount;
    
    /// <summary> Return the largest entity <see cref="Entity.Id"/> store in the entity store. </summary>
    [Browse(Never)] public              int                     NodeMaxId       => nodesMaxId;
    

    
    /// <summary> Initialize a default <see cref="ParallelJobRunner"/> used for <see cref="QueryJob"/>'s. </summary>
    [Browse(Never)] public              ParallelJobRunner       JobRunner       { get; init; }

    /// <summary>Array of <see cref="Archetype"/>'s utilized by the entity store</summary>
    /// <remarks>Each <see cref="Archetype"/> contains all entities of a specific combination of <b>struct</b> components.</remarks>
                    public ReadOnlySpan<Archetype>              Archetypes      => new (archs, 0, archsCount);
    
    /// <summary> Returns the current number of <see cref="Archetypes"/> managed by the entity store. </summary>
    [Browse(Never)] public              int                     ArchetypeCount  => archsCount;
    
    [Browse(Never)] internal     Action<ComponentChanged>       ComponentAdded  => internBase.componentAdded;
    [Browse(Never)] internal     Action<ComponentChanged>       ComponentRemoved=> internBase.componentRemoved;
    [Browse(Never)] internal     Action<TagsChanged>            TagsChanged     => internBase.tagsChanged;

                    public   override   string                  ToString()      => $"entities: {entityCount}";
    #endregion

#region events
    /// <summary>Add / remove an event handler for <see cref="ECS.TagsChanged"/> events triggered by:<br/>
    /// <see cref="Entity.AddTag{T}"/> <br/> <see cref="Entity.AddTags"/> <br/> <see cref="Entity.RemoveTag{T}"/> <br/> <see cref="Entity.RemoveTags"/>.</summary>
    public event    Action<TagsChanged>       OnTagsChanged      { add => internBase.tagsChanged      += value; remove => internBase.tagsChanged      -= value; }
    
    /// <summary>Add / remove an event handler for <see cref="ECS.ComponentChanged"/> events triggered by: <br/>
    /// <see cref="Entity.AddComponent{T}()"/>.</summary>
    public event    Action<ComponentChanged>  OnComponentAdded   { add => internBase.componentAdded   += value; remove => internBase.componentAdded   -= value; }
    
    /// <summary>Add / remove an event handler for <see cref="ECS.ComponentChanged"/> events triggered by: <br/>
    /// <see cref="Entity.RemoveComponent{T}()"/>.</summary>
    public event    Action<ComponentChanged>  OnComponentRemoved { add => internBase.componentRemoved += value; remove => internBase.componentRemoved -= value; }
    #endregion
    
#region private / internal fields
    // --- archetypes
    [Browse(Never)] protected           Archetype[]             archs;              //  8   - array of all archetypes. never null
    [Browse(Never)] private             int                     archsCount;         //  4   - number of archetypes
    [Browse(Never)] private  readonly   HashSet<ArchetypeKey>   archSet;            //  8   - Set<> to get archetypes by key
    /// <summary>The default <see cref="Archetype"/> has no <see cref="Archetype.ComponentTypes"/> and <see cref="Archetype.Tags"/>.<br/>
    /// Its <see cref="Archetype"/>.<see cref="Archetype.archIndex"/> is always 0 (<see cref="Static.DefaultArchIndex"/>).</summary>
    [Browse(Never)] internal readonly   Archetype               defaultArchetype;   //  8   - default archetype. has no components & tags
    // --- nodes
    [Browse(Never)] protected           int                     nodesMaxId;         //  4   - highest entity id
    [Browse(Never)] internal            int                     entityCount;        //  4   - number of all entities
    // --- misc
    [Browse(Never)] private   readonly  ArchetypeKey            searchKey;          //  8   - key buffer to find archetypes by key
    
                    private             InternBase              internBase;         // 40
    /// <summary>Contains state of <see cref="EntityStoreBase"/> not relevant for application development.</summary>
    /// <remarks>Declaring internal state fields in this struct remove noise in debugger.</remarks>
    // MUST be private by all means 
    private struct InternBase {
        // --- delegates
        internal        Action                <TagsChanged>         tagsChanged;            //  8   - fires event if entity Tags are changed
        internal        Dictionary<int, Action<TagsChanged>>        entityTagsChanged;      //  8   - entity event handlers for add/remove Tags
        //
        internal        Action                <ComponentChanged>    componentAdded;         //  8   - fires event on add component
        internal        Action                <ComponentChanged>    componentRemoved;       //  8   - fires event on remove component
        internal        Dictionary<int, Action<ComponentChanged>>   entityComponentChanged; //  8   - entity event handlers for add/remove component
        //
        internal        StackArray<EntityBatch>                     entityBatches;          //  8
        internal        StackArray<CreateEntityBatch>               createEntityBatches;    //  8
        internal        StackArray<EntityList>                      entityLists;            //  8
    }
    #endregion
    
#region static fields
    // use nested class to minimize noise in debugger
    internal static class Static
    {
        internal static readonly    EntitySchema    EntitySchema    = SchemaUtils.RegisterSchemaTypes();
        /// <summary>All items in the <see cref="DefaultHeapMap"/> are always null</summary>
        internal static readonly    StructHeap[]    DefaultHeapMap  = new StructHeap[EntitySchema.maxStructIndex];
        
        /// <summary>The index of the <see cref="EntityStoreBase.defaultArchetype"/> - index is always 0</summary>
        internal const              int             DefaultArchIndex        =  0;
        
        /// <summary>to avoid accidental entity access by id using (default value) 0 </summary>
        internal const              int             MinNodeId               =  1;
    }
    #endregion
    
#region initialize
    protected EntityStoreBase()
    {
        archs               = new Archetype[2];
        archSet             = new HashSet<ArchetypeKey>(ArchetypeKeyEqualityComparer.Instance);
        var config          = GetArchetypeConfig(this);
        defaultArchetype    = new Archetype(config);
        searchKey           = new ArchetypeKey();
        AddArchetype(this, defaultArchetype);
        internBase.entityBatches        = new StackArray<EntityBatch>       (Array.Empty<EntityBatch>());
        internBase.createEntityBatches  = new StackArray<CreateEntityBatch> (Array.Empty<CreateEntityBatch>());
        internBase.entityLists          = new StackArray<EntityList>        (Array.Empty<EntityList>());
    }
    
    protected internal abstract void    UpdateEntityCompIndex(int id, int compIndex);
    
    #endregion
    
    
    
#region exceptions
    private static ArgumentException ArgumentException(string message, string parameterName) {
        // required as Unity format exception message different than CLR
        return new ArgumentException($"{message} (Parameter '{parameterName}')");
    }

    internal static Exception   InvalidStoreException(string parameterName) {
        return ArgumentException("entity is owned by a different store", parameterName);
    }
        
    internal static Exception   InvalidEntityIdException(int id, string parameterName) {
        return ArgumentException($"invalid entity id <= 0. was: {id}", parameterName);
    }
        
    internal static Exception   IdAlreadyInUseException(int id, string parameterName) {
        return ArgumentException($"id already in use in EntityStore. id: {id}", parameterName);
    }
    #endregion
}
