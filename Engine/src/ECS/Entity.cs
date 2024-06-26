﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static System.Diagnostics.DebuggerBrowsableState;
using static Friflo.Engine.ECS.StoreOwnership;
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
/// <see cref="ECS.Tags"/> and <see cref="IComponent"/>'s.<br/>
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
///     <description>tags</description>
///     <description><see cref="OnTagsChanged"/></description>
///     <description><see cref="TagsChanged"/></description>
///     <description>
///       <see cref="TagsChanged.AddedTags"/>, <see cref="TagsChanged.RemovedTags"/>
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
/// <item>  <b>tags</b>                 <br/>
///     <see cref="Tags"/>              <br/>
///     <see cref="AddTag{T}"/>         <br/>
///     <see cref="AddTags"/>           <br/>
///     <see cref="RemoveTag{T}"/>      <br/>
///     <see cref="RemoveTags"/>        <br/>
/// </item>
/// <item>  <b>child entities</b>       <br/>
///     <see cref="DeleteEntity"/>      <br/>
/// </item>
/// <item>  <b>events</b>                           <br/>
///     <see cref="OnTagsChanged"/>                 <br/>
///     <see cref="OnComponentChanged"/>            <br/>
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
    #endregion




    // ------------------------------------ child / tree methods ----------------------------------
#region child / tree - methods
    /// <summary>
    /// Delete the entity from its <see cref="EntityStore"/>.<br/>
    /// The deleted instance is in <see cref="detached"/> state.
    /// Calling <see cref="Entity"/> methods result in <see cref="NullReferenceException"/>'s
    /// </summary>
    /// <remarks>
    /// Executes in O(1)
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

    // Deprecated comment. Was valid when Entity was a class
    // [c# - What is the memory overhead of a .NET Object - Stack Overflow]     // 16 overhead for reference type on x64
    // https://stackoverflow.com/questions/10655829/what-is-the-memory-overhead-of-a-net-object/10655864#10655864
    #endregion
}
