﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static System.Diagnostics.DebuggerBrowsableState;
using static Friflo.Engine.ECS.StoreOwnership;
using static Friflo.Engine.ECS.TreeMembership;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

namespace Friflo.Engine.ECS;

/// <summary>
/// Represent an object in an <see cref="EntityStore"/> - e.g. a cube in a game scene.<br/>
/// It is the <b>main API</b> to deal with entities in the engine.<br/>
/// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#entity">Example.</a>
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="Entity"/> has an <see cref="Id"/> and is a container of
/// <see cref="ECS.Tags"/>, <see cref="IComponent"/>'s, <see cref="Script"/>'s and other child <see cref="Entity"/>'s.<br/>
/// <br/>
/// Comparison to other game engines.
/// <list type="bullet">
///     <item>
///         <b>Unity</b>  - an <see cref="Entity"/> provides a similar features set as a <c>GameObject</c> and their ECS <c>Entity</c>.
///     </item>
///     <item>
///         <b>Godot</b>  - <see cref="Entity"/> is the counterpart of a <c>Node</c>.<br/>
///         The key difference is Godot is an OOP architecture inheriting from <c>Node</c> over multiple levels.
///     </item>
///     <item>
///         <b>FLAX</b>   - <see cref="Entity"/> is the counterpart of an <c>Actor</c> - an OOP architecture like Godot.
///     </item>
///     <item>
///         <b>STRIDE</b> - <see cref="Entity"/> is the counterpart of a STRIDE <c>Entity</c> - a component based architecture like Unity.<br/>
///         In contrast to this engine or Unity it has no ECS architecture - Entity Component System.
///     </item>
/// </list>
/// </para>
/// <para>
/// <b>Components</b>
/// <br/>
/// An <see cref="Entity"/> is typically an object that can be rendered on screen like a cube, sphere, capsule, mesh, sprite, ... .<br/>
/// Therefore a renderable component needs to be added with <see cref="AddComponent{T}()"/> to an <see cref="Entity"/>.<br/>
/// <br/>
/// <b>Child entities</b>
/// <br/>
/// An <see cref="Entity"/> can be added to another <see cref="Entity"/> using <see cref="AddChild"/>.<br/>
/// The added <see cref="Entity"/> becomes a child of the <see cref="Entity"/> it is added to - its <see cref="Parent"/>.<br/>
/// This enables to build up a complex game scene with a hierarchy of <see cref="Entity"/>'s.<br/>
/// The order of children contained by an entity is the insertion order.<br/>  
/// <br/>
/// <b>Scripts</b>
/// <br/>
/// A <see cref="Script"/>'s can be added to an <see cref="Entity"/> to add custom logic (script) and data to an entity.<br/>
/// <see cref="Script"/>'s are added or removed with <see cref="AddScript{T}"/> / <see cref="RemoveScript{T}"/>.<br/>
/// <br/>
/// <b>Tags</b>
/// <br/>
/// <see cref="Tags"/> can be added to an <see cref="Entity"/> to enable filtering entities in queries.<br/>
/// By adding <see cref="Tags"/> to an <see cref="ArchetypeQuery"/> it can be restricted to return only entities matching the
/// these <see cref="Tags"/>.<br/>
/// <br/>
/// <b>Events</b>
/// <br/>
/// All entity changes - aka mutations - can be observed for specific <see cref="Entity"/>'s and the whole <see cref="EntityStore"/>.<br/>
/// In detail the following changes can be observed.
/// <list type="table">
///   <listheader>
///     <term>type</term>
///     <term>entity event</term>
///     <term>event argument</term>
///     <term>action</term>
///   </listheader>
///   <item>
///     <description>component</description>
///     <description><see cref="OnComponentChanged"/></description>
///     <description><see cref="ComponentChanged"/></description>
///     <description>
///       <see cref="ComponentChangedAction.Add"/>, <see cref="ComponentChangedAction.Update"/>, <see cref="ComponentChangedAction.Remove"/>
///     </description>
///   </item>
///   <item>
///     <description>script</description>
///     <description><see cref="OnScriptChanged"/></description>
///     <description><see cref="ScriptChanged"/></description>
///     <description>
///       <see cref="ScriptChangedAction.Remove"/>, <see cref="ScriptChangedAction.Add"/>, <see cref="ScriptChangedAction.Replace"/>
///     </description>
///   </item>
///   <item>
///     <description>tags</description>
///     <description><see cref="OnTagsChanged"/></description>
///     <description><see cref="TagsChanged"/></description>
///     <description>
///       <see cref="TagsChanged.AddedTags"/>, <see cref="TagsChanged.RemovedTags"/>
///     </description>
///   </item>
///   <item>
///     <description>child entity</description>
///     <description><see cref="OnChildEntitiesChanged"/></description>
///     <description><see cref="ChildEntitiesChanged"/></description>
///     <description>
///       <see cref="ChildEntitiesChangedAction.Add"/>, <see cref="ChildEntitiesChangedAction.Remove"/>
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Properties and Methods by category</b>
/// <list type="bullet">
/// <item>  <b>general</b>      <br/>
///     <see cref="Id"/>        <br/>
///     <see cref="Pid"/>       <br/>
///     <see cref="Archetype"/> <br/>
///     <see cref="Store"/>     <br/>
/// </item>
/// <item>  <b>components</b> · generic             <br/>
///     <see cref="HasComponent{T}"/>               <br/>
///     <see cref="GetComponent{T}"/> - read / write<br/>
///     <see cref="TryGetComponent{T}"/>            <br/>
///     <see cref="AddComponent{T}()"/>             <br/>
///     <see cref="RemoveComponent{T}"/>            <br/>
/// </item>
/// <item>  <b>scripts</b>              <br/>
///     <see cref="Scripts"/>           <br/>
///     <see cref="GetScript{T}"/>      <br/>
///     <see cref="TryGetScript{T}"/>   <br/>
///     <see cref="AddScript{T}"/>      <br/>
///     <see cref="RemoveScript{T}"/>   <br/>
/// </item>
/// <item>  <b>tags</b>                 <br/>
///     <see cref="Tags"/>              <br/>
///     <see cref="AddTag{T}"/>         <br/>
///     <see cref="AddTags"/>           <br/>
///     <see cref="RemoveTag{T}"/>      <br/>
///     <see cref="RemoveTags"/>        <br/>
/// </item>
/// <item>  <b>child entities</b>       <br/>
///     <see cref="Parent"/>            <br/>
///     <see cref="ChildEntities"/>     <br/>
///     <see cref="ChildIds"/>          <br/>
///     <see cref="ChildCount"/>        <br/>
///     <see cref="AddChild"/>          <br/>
///     <see cref="InsertChild"/>       <br/>
///     <see cref="RemoveChild"/>       <br/>
///     <see cref="DeleteEntity"/>      <br/>
///     <see cref="GetChildIndex"/>     <br/>
/// </item>
/// <item>  <b>events</b>                           <br/>
///     <see cref="OnTagsChanged"/>                 <br/>
///     <see cref="OnComponentChanged"/>            <br/>
///     <see cref="OnScriptChanged"/>               <br/>
///     <see cref="OnChildEntitiesChanged"/>        <br/>
///     <see cref="DebugEventHandlers"/>            <br/>
/// </item>
/// <item>  <b>signals</b>                          <br/>
///     <see cref="AddSignalHandler{TEvent}"/>      <br/>
///     <see cref="RemoveSignalHandler{TEvent}"/>   <br/>
///     <see cref="EmitSignal{TEvent}"/>            <br/>
/// </item>
/// </list>
/// </para>
/// </remarks>
[CLSCompliant(true)]
public readonly struct Entity : IEquatable<Entity>
{
    // ------------------------------------ general properties ------------------------------------
#region general properties
    /// <summary>Returns the permanent entity id used for serialization.</summary>
    [Browse(Never)]
    public              long                    Pid             => store.nodes[Id].pid;

    /// <summary>Return the <see cref="IComponent"/>'s added to the entity.</summary>
    public              EntityComponents        Components      => new EntityComponents(this);
    
    /// <summary>Return the <see cref="Script"/>'s added to the entity.</summary>
    public              Scripts                 Scripts         => new Scripts(EntityUtils.GetScripts(this));

    /// <summary>Return the <see cref="ECS.Tags"/> added to the entity.</summary>
    /// <returns>
    /// A copy of the <see cref="Tags"/> assigned to the <see cref="Entity"/>.<br/>
    /// <br/>
    /// Modifying the returned <see cref="ECS.Tags"/> value does <b>not</b> affect the <see cref="Entity"/>.<br/>
    /// Therefore use <see cref="AddTag{T}"/>, <see cref="AddTags"/>, <see cref="RemoveTag{T}"/> or <see cref="RemoveTags"/>.
    /// </returns>
    public     ref readonly Tags                Tags            => ref archetype.tags;

    /// <summary>Returns the <see cref="Archetype"/> that contains the entity.</summary>
    /// <remarks>The <see cref="Archetype"/> the entity is stored.<br/>Return null if the entity is <see cref="detached"/></remarks>
    public                  Archetype           Archetype       => archetype;
    
    /// <summary>Returns the <see cref="EntityStore"/> that contains the entity.</summary>
    /// <remarks>The <see cref="Store"/> the entity is <see cref="attached"/> to. Returns null if <see cref="detached"/></remarks>
    [Browse(Never)] public  EntityStore         Store           => archetype?.entityStore;
                    
    /// <remarks>If <see cref="attached"/> its <see cref="Store"/> and <see cref="Archetype"/> are not null. Otherwise null.</remarks>
    [Browse(Never)] public  StoreOwnership      StoreOwnership  => archetype != null ? attached : detached;
    
    /// <returns>
    /// <see cref="treeNode"/> if the entity is member of the <see cref="EntityStore"/> tree graph.<br/>
    /// Otherwise <see cref="floating"/></returns>
    [Browse(Never)] public  TreeMembership      TreeMembership  => archetype.entityStore.GetTreeMembership(Id);
    
    /// <summary> Returns true if the entity was deleted. </summary>
    [Browse(Never)] public  bool                IsNull          => store?.nodes[Id].archetype == null;
    
    /// <summary> Display additional entity information like Pid, Enabled, JSON and attached event handlers.</summary>
                    internal EntityInfo         Info => new EntityInfo(this);
    
    /// <summary>
    /// Set entity to enabled/disabled by removing/adding the <see cref="Disabled"/> tag.<br/>
    /// </summary>
    [Browse(Never)] public   bool               Enabled
                    { get => !Tags.HasAll(EntityUtils.Disabled); set { if (value) RemoveTags(EntityUtils.Disabled); else AddTags(EntityUtils.Disabled); } }
    #endregion





    // ------------------------------------ child / tree properties -------------------------------
#region child / tree - properties
    /// <summary>Return the number of child entities.</summary>
    [Browse(Never)] public  int                 ChildCount  => archetype.entityStore.nodes[Id].childCount;
    
    /// <summary>Returns the parent entity that contains the entity.</summary>
    /// <returns>
    /// null if the entity has no parent.<br/>
    /// <i>Note:</i>The <see cref="EntityStore"/>.<see cref="EntityStore.StoreRoot"/> returns always null
    /// </returns>
    /// <remarks>Executes in O(1)</remarks> 
                    public  Entity              Parent      => EntityStore.GetParent(archetype.entityStore, Id);
    
    /// <summary>Return all child entities of an entity.</summary>
    /// <remarks>
    /// Executes in O(1).<br/> Enumerate with:
    /// <code>
    ///     foreach (var child in entity.ChildEntities)
    /// </code>
    /// </remarks>
     public  ChildEntities       ChildEntities   => EntityStore.GetChildEntities(archetype.entityStore, Id);
    
    /// <summary>Return the ids of the child entities.</summary>
    [Browse(Never)] public  ReadOnlySpan<int>   ChildIds        => EntityStore.GetChildIds(archetype.entityStore, Id);
    #endregion




    // ------------------------------------ fields ------------------------------------------------
#region fields
    // Note! Must not have any other fields to keep its size at 16 bytes
    [Browse(Never)] internal    readonly    EntityStore store;  //  8
    /// <summary>Unique entity id.<br/>
    /// Uniqueness relates to the <see cref="Entity"/>'s stored in its <see cref="EntityStore"/></summary>
    // ReSharper disable once InconsistentNaming
                    public      readonly    int         Id;     //  4
    #endregion




    // ------------------------------------ component methods -------------------------------------
#region component - methods
    /// <summary>Return true if the entity contains a component of the given type.</summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public  bool    HasComponent<T> ()  where T : struct, IComponent  => archetype.heapMap[StructHeap<T>.StructIndex] != null;

    /// <summary>Return the component of the given type as a reference.</summary>
    /// <exception cref="NullReferenceException"> if entity has no component of Type <typeparamref name="T"/></exception>
    /// <remarks>Executes in O(1)</remarks>
    public  ref T   GetComponent<T>()   where T : struct, IComponent
    => ref ((StructHeap<T>)archetype.heapMap[StructHeap<T>.StructIndex]).components[compIndex];
    
    /// <remarks>Executes in O(1)</remarks>
    public bool     TryGetComponent<T>(out T result) where T : struct, IComponent
    {
        var heap = archetype.heapMap[StructHeap<T>.StructIndex];
        if (heap == null) {
            result = default;
            return false;
        }
        result = ((StructHeap<T>)heap).components[compIndex];
        return true;
    }
    /// <summary>
    /// Add a component of the given type <typeparamref name="T"/> to the entity.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#component">Example.</a>
    /// <br/>
    /// If the entity contains a component of the same type it is updated.</summary>
    /// <returns>true - component is newly added to the entity.<br/> false - component is updated.</returns>
    /// <remarks>Note: Use <see cref="EntityUtils.AddEntityComponent"/> as non generic alternative</remarks>
    public bool AddComponent<T>()               where T : struct, IComponent {
        int archIndex = 0;
        return EntityStoreBase.AddComponent<T>(Id, StructHeap<T>.StructIndex, ref refArchetype, ref refCompIndex, ref archIndex, default);
    }
    /// <summary>
    /// Add the given <paramref name="component"/> to the entity.<br/>
    /// If the entity contains a component of the same type it is updated.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#component">Example.</a>
    /// </summary>
    /// <returns>true - component is newly added to the entity.<br/> false - component is updated.</returns>
    public bool AddComponent<T>(in T component) where T : struct, IComponent {
        int archIndex = 0;
        return EntityStoreBase.AddComponent   (Id, StructHeap<T>.StructIndex, ref refArchetype, ref refCompIndex, ref archIndex, in component);
    }
    /// <summary>Remove the component of the given type from the entity.</summary>
    /// <returns>true if entity contained a component of the given type before</returns>
    /// <remarks>
    /// Executes in O(1)<br/>
    /// <remarks>Note: Use <see cref="EntityUtils.RemoveEntityComponent"/> as non generic alternative</remarks>
    /// </remarks>
    public bool RemoveComponent<T>()            where T : struct, IComponent {
        int archIndex = 0;
        return EntityStoreBase.RemoveComponent<T>(Id, ref refArchetype, ref refCompIndex, ref archIndex, StructHeap<T>.StructIndex);
    }
    #endregion




    // ------------------------------------ script methods ----------------------------------------
#region script - methods
    /// <summary>Get the script of the passed <typeparamref name="TScript"/> <see cref="Type"/>.</summary>
    /// <returns>null if the entity has no script of the passed <typeparamref name="TScript"/> <see cref="Type"/>.</returns>
    /// <remarks>Note: Use <see cref="EntityUtils.GetEntityScript"/> as non generic alternative.</remarks> 
    public TScript        GetScript<TScript>()        where TScript : Script, new() {
        return (TScript)EntityUtils.GetScript(this, typeof(TScript));
    }
    /// <summary>Gets the script with the passed <typeparamref name="TScript"/> <see cref="Type"/>.</summary>
    /// <returns>
    /// Returns true if the entity has a script the passed <typeparamref name="TScript"/> <see cref="Type"/>.<br/>
    /// Otherwise false.
    /// </returns>
    public bool     TryGetScript<TScript>(out TScript result)
        where TScript : Script, new()
    {
        result = (TScript)EntityUtils.GetScript(this, typeof(TScript));
        return result != null;
    }
    /// <summary>Add the given <paramref name="script"/> to the entity.<br/>
    /// If the entity contains a script of the same <typeparamref name="TScript"/> <see cref="Type"/> it is replaced.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#script">Example.</a>
    /// </summary>
    /// <returns>
    /// The script with the passed <typeparamref name="TScript"/> <see cref="Type"/> previously added to the entity.<br/>
    /// Return null if the entity had no script with the passed <typeparamref name="TScript"/> <see cref="Type"/>.
    /// </returns>
    /// <remarks>Note: Use <see cref="EntityUtils.AddNewEntityScript"/> as non generic alternative.</remarks>
    public TScript  AddScript<TScript>(TScript script)   where TScript : Script, new() {
        return (TScript)EntityUtils.AddScript    (this, ScriptType<TScript>.Index, script);
    }
    /// <summary>Remove the script with the given <typeparamref name="TScript"/> <see cref="Type"/> from the entity.</summary>
    /// <returns>
    /// The script the script with the passed <typeparamref name="TScript"/> <see cref="Type"/> previously added to the entity.<br/>
    /// Or null if the entity has no script with the passed <typeparamref name="TScript"/> <see cref="Type"/>.
    /// </returns>
    /// <remarks>Note: Use <see cref="EntityUtils.RemoveEntityScript"/> as non generic alternative.</remarks>
    public TScript        RemoveScript<TScript>()        where TScript : Script, new() {
        return (TScript)EntityUtils.RemoveScript (this, ScriptType<TScript>.Index);
    }
    #endregion




    // ------------------------------------ tag methods -------------------------------------------
#region tag - methods
    // Note: no query Tags methods like HasTag<T>() here by intention. Tags offers query access
    /// <summary>
    /// Add the given <typeparamref name="TTag"/> to the entity.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#tag">Example.</a>
    /// </summary>
    public bool AddTag<TTag>()    where TTag : struct, ITag {
        int index = 0;
        return EntityStoreBase.AddTags   (archetype.store, Tags.Get<TTag>(), Id, ref refArchetype, ref refCompIndex, ref index);
    }
    /// <summary>Add the given <paramref name="tags"/> to the entity.</summary>
    public bool AddTags(in Tags tags) {
        int index = 0;
        return EntityStoreBase.AddTags   (archetype.store, tags,          Id, ref refArchetype, ref refCompIndex, ref index);
    }
    /// <summary>Add the given <typeparamref name="TTag"/> from the entity.</summary>
    public bool RemoveTag<TTag>() where TTag : struct, ITag {
        int index = 0;
        return EntityStoreBase.RemoveTags(archetype.store, Tags.Get<TTag>(), Id, ref refArchetype, ref refCompIndex, ref index);
    }
    /// <summary>Remove the given <paramref name="tags"/> from the entity.</summary>
    public bool RemoveTags(in Tags tags) {
        int index = 0;
        return EntityStoreBase.RemoveTags(archetype.store, tags,          Id, ref refArchetype, ref refCompIndex, ref index);
    }
    
    /// <summary> Enable recursively all child entities of the <see cref="Entity"/>. </summary>
    public void EnableTree()  => EntityUtils.RemoveTreeTags(this, EntityUtils.Disabled);
    
    /// <summary> Disable recursively all child entities of the <see cref="Entity"/>. </summary>
    public void DisableTree() => EntityUtils.AddTreeTags(this, EntityUtils.Disabled);
    #endregion




    // ------------------------------------ child / tree methods ----------------------------------
#region child / tree - methods
    /// <summary>
    /// Add the given <paramref name="entity"/> as a child to the entity.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#child-entities">Example.</a>
    /// </summary>
    /// <remarks>
    /// Executes in O(1).<br/>If its <see cref="TreeMembership"/> changes O(number of nodes in sub tree).<br/>
    /// The subtree structure of the added entity remains unchanged<br/>
    /// </remarks>
    /// <returns>
    /// The index within <see cref="ChildIds"/> the <paramref name="entity"/> is added.<br/>
    /// -1 if the <paramref name="entity"/> is already a child entity.
    /// </returns>
    public int AddChild(Entity entity) {
        var entityStore = archetype.entityStore;
        if (entityStore != entity.archetype.store) throw EntityStoreBase.InvalidStoreException(nameof(entity));
        return entityStore.AddChild(Id, entity.Id);
    }
    /// <summary>Insert the given <paramref name="entity"/> as a child to the entity at the passed <paramref name="index"/>.</summary>
    /// <remarks>
    /// Executes in O(1) in case <paramref name="index"/> == <see cref="ChildCount"/>.<br/>
    /// Otherwise O(N). N = <see cref="ChildCount"/> - <paramref name="index"/><br/>
    /// If its <see cref="TreeMembership"/> changes O(number of nodes in sub tree).<br/>
    /// The subtree structure of the added entity remains unchanged<br/>
    /// </remarks>
    public void InsertChild(int index, Entity entity) {
        var entityStore = archetype.entityStore;
        if (entityStore != entity.archetype.store) throw EntityStoreBase.InvalidStoreException(nameof(entity));
        entityStore.InsertChild(Id, entity.Id, index);
    }
    /// <summary>Remove the given child <paramref name="entity"/> from the entity.</summary>
    /// <remarks>
    /// Executes in O(N) to search the entity. N = <see cref="ChildCount"/><br/>
    /// If its <see cref="TreeMembership"/> changes (in-tree / floating) O(number of nodes in sub tree).<br/>
    /// The subtree structure of the removed entity remains unchanged<br/>
    /// </remarks>
    public bool RemoveChild(Entity entity) {
        var entityStore = archetype.entityStore;
        if (entityStore != entity.archetype.store) throw EntityStoreBase.InvalidStoreException(nameof(entity));
        return entityStore.RemoveChild(Id, entity.Id);
    }
    
    /// <summary>
    /// Delete the entity from its <see cref="EntityStore"/>.<br/>
    /// The deleted instance is in <see cref="detached"/> state.
    /// Calling <see cref="Entity"/> methods result in <see cref="NullReferenceException"/>'s
    /// </summary>
    /// <remarks>
    /// Executes in O(1) in case the entity has no children and if it is the last entity in <see cref="Parent"/>.<see cref="ChildIds"/>
    /// </remarks>
    public void DeleteEntity()
    {
        try {
            // Send event. See: SEND_EVENT notes. Note - Specific characteristic: event is send before deleting the entity.
            store.DeleteEntityEvent(this);
        }
        finally {
            var arch            = archetype;
            var componentIndex  = compIndex;
            var entityStore     = arch.entityStore;
            entityStore.DeleteNode(Id); 
            Archetype.MoveLastComponentsTo(arch, componentIndex);
        }
    }
    /// <summary>Return the position of the given <paramref name="child"/> in the entity.</summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public int  GetChildIndex(Entity child)     => archetype.entityStore.GetChildIndex(Id, child.Id);    
    #endregion




    // ------------------------------------ general methods ---------------------------------------
#region general - methods
    /// <summary> Return true if the passed entities have the same <see cref="Entity.Id"/>'s. </summary>
    public static   bool    operator == (Entity a, Entity b)    => a.Id == b.Id && a.store == b.store;
    
    /// <summary> Return true if the passed entities have the different <see cref="Entity.Id"/>'s. </summary>
    public static   bool    operator != (Entity a, Entity b)    => a.Id != b.Id || a.store != b.store;

    // --- IEquatable<T>
    public          bool    Equals(Entity other)                => Id == other.Id && store == other.store;

    // --- object
    /// <summary> Note: Not implemented to avoid excessive boxing. </summary>
    /// <remarks> Use <see cref="operator=="/> or <see cref="EntityUtils.EqualityComparer"/> </remarks>
    public override bool    Equals(object obj)  => throw EntityUtils.NotImplemented(Id, "== Equals(Entity)");
    
    /// <summary> Note: Not implemented to avoid excessive boxing. </summary>
    /// <remarks> Use <see cref="Id"/> or <see cref="EntityUtils.EqualityComparer"/> </remarks>
    public override int     GetHashCode()       => throw EntityUtils.NotImplemented(Id, nameof(Id));
    
    public override string  ToString()          => EntityUtils.EntityToString(this);
    
    internal Entity(EntityStore entityStore, int id) {
        store   = entityStore;
        Id      = id;
    }

    /// <summary>
    /// Returns an <see cref="EntityBatch"/> to add/remove components or tags to/from this entity using the batch.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#batch---entity">Example.</a>
    /// </summary>
    /// <remarks>
    /// The returned batch is used to add/removed components and tags.<br/>
    /// These changes are applied to the entity when calling <see cref="EntityBatch.Apply"/>.<br/>
    /// <br/>
    /// Subsequent use of the batch throws <see cref="BatchAlreadyAppliedException"/>.<br/>
    /// <br/>
    /// If missing the <see cref="EntityBatch.Apply"/> call:<br/>
    /// - Entity changes are not applied.<br/>
    /// - Some unnecessary memory allocations.<br/>
    /// <br/>
    /// When calling <see cref="EntityBatch.Apply"/> the batch executes without memory allocations.
    /// </remarks>
    public EntityBatch Batch() => store.GetBatch(Id);
    #endregion




    // ------------------------------------ events ------------------------------------------------
#region events
    /// <summary>
    /// Add / remove an event handler for <see cref="TagsChanged"/> events triggered by:<br/>
    /// <see cref="AddTag{T}"/> <br/> <see cref="AddTags"/> <br/> <see cref="RemoveTag{T}"/> <br/> <see cref="RemoveTags"/>.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#event">Example.</a>
    /// </summary>
    public event Action<TagsChanged>            OnTagsChanged           { add    => EntityStoreBase.AddEntityTagsChangedHandler     (store, Id, value);
                                                                          remove => EntityStoreBase.RemoveEntityTagsChangedHandler  (store, Id, value);  }
    /// <summary>
    /// Add / remove an event handler for <see cref="ComponentChanged"/> events triggered by: <br/>
    /// <see cref="AddComponent{T}()"/> <br/> <see cref="RemoveComponent{T}"/>.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#event">Example.</a>
    /// </summary>
    public event Action<ComponentChanged>       OnComponentChanged      { add    => EntityStoreBase.AddComponentChangedHandler      (store, Id, value);
                                                                          remove => EntityStoreBase.RemoveComponentChangedHandler   (store, Id, value);  }
    /// <summary>
    /// Add / remove an event handler for <see cref="ScriptChanged"/> events triggered by:<br/>
    /// <see cref="AddScript{T}"/> <br/> <see cref="RemoveScript{T}"/>.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#event">Example.</a>
    /// </summary>
    public event Action<ScriptChanged>          OnScriptChanged         { add    => EntityStore.AddScriptChangedHandler             (store, Id, value);
                                                                          remove => EntityStore.RemoveScriptChangedHandler          (store, Id, value);  }
    /// <summary>
    /// Add / remove an event handler for <see cref="ChildEntitiesChanged"/> events triggered by:<br/>
    /// <see cref="AddChild"/> <br/> <see cref="InsertChild"/> <br/> <see cref="RemoveChild"/>.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#event">Example.</a>
    /// </summary>
    public event Action<ChildEntitiesChanged>   OnChildEntitiesChanged  { add    => EntityStore.AddChildEntitiesChangedHandler      (store, Id, value);
                                                                          remove => EntityStore.RemoveChildEntitiesChangedHandler   (store, Id, value);  }
    
    /// <summary>
    /// Add the given <see cref="Signal{TEvent}"/> handler to the entity.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#signal">Example.</a>
    /// </summary>
    /// <returns>The the signal handler added to the entity.<br/>
    /// Practical when passing a lambda that can be removed later with <see cref="RemoveSignalHandler{TEvent}"/>.</returns>
    public Action<Signal<TEvent>>  AddSignalHandler   <TEvent> (Action<Signal<TEvent>> handler) where TEvent : struct {
        EntityStore.AddSignalHandler   (store, Id, handler);
        return handler;
    }
    /// <summary>Remove the given <see cref="Signal{TEvent}"/> handler from the entity.</summary>
    /// <returns><c>true</c> in case the the passed signal handler was found.</returns>
    public bool  RemoveSignalHandler<TEvent> (Action<Signal<TEvent>> handler) where TEvent : struct {
        return EntityStore.RemoveSignalHandler(store, Id, handler);
    }

    /// <summary>
    /// Emits the passed signal event to all signal handlers added with <see cref="AddSignalHandler{TEvent}"/>.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#signal">Example.</a>
    /// </summary>
    /// <remarks> It executes in ~10 nano seconds per signal handler. </remarks>
    public void  EmitSignal<TEvent> (in TEvent ev) where TEvent : struct {
        var signalHandler = EntityStore.GetSignalHandler<TEvent>(store, Id);
        signalHandler?.Invoke(new Signal<TEvent>(store, Id, ev));
    }
    /// <summary> Return event and signal handlers added to the entity.</summary>
    /// <remarks> <b>Note</b>:
    /// Should be used only for debugging as it allocates arrays and do multiple Dictionary lookups.<br/>
    /// No allocations or lookups are made in case <see cref="ECS.DebugEventHandlers.TypeCount"/> is 0.
    /// </remarks>
    [Browse(Never)]
    public       DebugEventHandlers             DebugEventHandlers => EntityStore.GetEventHandlers(store, Id);
    #endregion




    // ------------------------------------ internal properties -----------------------------------
#region internal properties
    // ReSharper disable InconsistentNaming - placed on bottom to disable all subsequent hints
    /// <summary>The <see cref="Archetype"/> used to store the components of they the entity</summary>
    [Browse(Never)] internal    ref Archetype   refArchetype    => ref store.nodes[Id].archetype;
    [Browse(Never)] internal        Archetype      archetype    =>     store.nodes[Id].archetype;

    /// <summary>The index within the <see cref="refArchetype"/> the entity is stored</summary>
    /// <remarks>The index will change if entity is moved to another <see cref="Archetype"/></remarks>
    [Browse(Never)] internal    ref int         refCompIndex    => ref store.nodes[Id].compIndex;
    [Browse(Never)] internal        int            compIndex    =>     store.nodes[Id].compIndex;
    
    [Browse(Never)] internal    ref int         refScriptIndex  => ref store.nodes[Id].scriptIndex;
    [Browse(Never)] internal        int            scriptIndex  =>     store.nodes[Id].scriptIndex;

    // Deprecated comment. Was valid when Entity was a class
    // [c# - What is the memory overhead of a .NET Object - Stack Overflow]     // 16 overhead for reference type on x64
    // https://stackoverflow.com/questions/10655829/what-is-the-memory-overhead-of-a-net-object/10655864#10655864
    #endregion
}
