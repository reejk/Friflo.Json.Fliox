﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
using static Friflo.Engine.ECS.StructInfo;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// <see cref="ArchetypeQuery"/> and all its generic implementations are designed to be reused.
/// </summary>
public class ArchetypeQuery
{
#region public properties
    /// <summary>
    /// Return the number of entities matching the query.
    /// </summary>
    /// <remarks>
    /// Execution time O(matching <see cref="Archetypes"/>).<br/>
    /// Typically there are only a few matching <see cref="Archetypes"/>.
    /// </remarks>
    public              int                 EntityCount => Archetype.GetEntityCount(GetArchetypesSpan());
    
    /// <summary> Return the number of <c>Chunks</c> returned by the query. </summary>
    public              int                 ChunkCount  => Archetype.GetChunkCount (GetArchetypesSpan());
    
    /// <summary> Returns the set of <see cref="Archetype"/>'s matching the query.</summary>
    [DebuggerBrowsable(Never)]
    public ReadOnlySpan<Archetype>          Archetypes  => GetArchetypesSpan();

    /// <summary> The <see cref="EntityStore"/> on which the query operates. </summary>
    public              EntityStore         Store       => store as EntityStore;
    
    /// <summary>
    /// Return the <see cref="ArchetypeQuery"/> entities mainly for debugging.<br/>
    /// For efficient access to entity <see cref="IComponent"/>'s use one of the generic <c>EntityStore.Query()</c> methods. 
    /// </summary>
    public              QueryEntities       Entities    => new (this);
    
    /// <summary> An <see cref="ECS.EventFilter"/> used to filter the query result for added/removed components/tags. </summary>
    public              EventFilter         EventFilter => GetEventFilter();

    public override     string              ToString()  => GetString();
    #endregion

#region private / internal fields
    // --- non blittable types
    [Browse(Never)] private  readonly   EntityStoreBase     store;                  //  8
    [Browse(Never)] private             Archetype[]         archetypes;             //  8   current list of matching archetypes, can grow
    // --- blittable types
    [Browse(Never)] private             int                 archetypeCount;         //  4   current number archetypes 
    [Browse(Never)] private             int                 lastArchetypeCount;     //  4   number of archetypes the EntityStore had on last check
    [Browse(Never)] internal readonly   SignatureIndexes    signatureIndexes;       // 24   ordered struct indices of component types: T1,T2,T3,T4,T5
    [Browse(Never)] private  readonly   ComponentTypes      requiredComponents;     // 32
    
                    private             Tags                allTags;                // 32   entity must have all tags
                    private             Tags                anyTags;                // 32   entity must have any tag
                    private             Tags                withoutAllTags;         // 32   entity must not have all tags
                    private             Tags                withoutAnyTags;         // 32   entity must not have any tag
                    
                    private             ComponentTypes      allComponents;          // 32   entity must have all component types
                    private             ComponentTypes      anyComponents;          // 32   entity must have any component types
                    private             ComponentTypes      withoutAllComponents;   // 32   entity must not have all component types
                    private             ComponentTypes      withoutAnyComponents;   // 32   entity must not have any component types
    
    [Browse(Never)] private             int                 withoutAllTagsCount;    //  8
    [Browse(Never)] private             int                 anyTagsCount;           //  8
    [Browse(Never)] private             int                 allTagsCount;           //  8
    
    [Browse(Never)] private             int                 withoutAllComponentsCount;  //  8
    [Browse(Never)] private             int                 anyComponentsCount;         //  8
    [Browse(Never)] private             int                 allComponentsCount;         //  8
    [Browse(Never)] private             EventFilter         eventFilter;            //  8   used to filter component/tag add/remove events
    #endregion

#region methods
    // --- tags
    
    /// <summary> A query result will contain only entities having all passed <paramref name="tags"/>. </summary>
    /// <param name="tags"> Use <c>Tags.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   AllTags         (in Tags tags) { SetHasAllTags(tags); return this; }
    
    /// <summary> A query result will contain only entities having any of the the passed <paramref name="tags"/>. </summary>
    /// <param name="tags"> Use <c>Tags.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   AnyTags         (in Tags tags) { SetHasAnyTags(tags); return this; }
    
    /// <summary> Entities having all passed <paramref name="tags"/> are excluded from query result. </summary>
    /// <param name="tags"> Use <c>Tags.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   WithoutAllTags  (in Tags tags) { SetWithoutAllTags(tags); return this; }
    
    /// <summary> Entities having any of the passed <paramref name="tags"/> are excluded from query result. </summary>
    /// <param name="tags"> Use <c>Tags.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   WithoutAnyTags  (in Tags tags) { SetWithoutAnyTags(tags); return this; }
    
    
    // --- components
    
    /// <summary> A query result will contain only entities having all passed <paramref name="componentTypes"/>. </summary>
    /// <param name="componentTypes"> Use <c>ComponentTypes.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   AllComponents         (in ComponentTypes componentTypes) { SetHasAllComponents(componentTypes); return this; }
    
    /// <summary> A query result will contain only entities having any of the the passed <paramref name="componentTypes"/>. </summary>
    /// <param name="componentTypes"> Use <c>ComponentTypes.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   AnyComponents         (in ComponentTypes componentTypes) { SetHasAnyComponents(componentTypes); return this; }
    
    /// <summary> Entities having all passed <paramref name="componentTypes"/> are excluded from query result. </summary>
    /// <param name="componentTypes"> Use <c>ComponentTypes.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   WithoutAllComponents  (in ComponentTypes componentTypes) { SetWithoutAllComponents(componentTypes); return this; }
    
    /// <summary> Entities having any of the passed <paramref name="componentTypes"/> are excluded from query result. </summary>
    /// <param name="componentTypes"> Use <c>ComponentTypes.Get&lt;>()</c> to set the parameter. </param>
    public ArchetypeQuery   WithoutAnyComponents  (in ComponentTypes componentTypes) { SetWithoutAnyComponents(componentTypes); return this; }
    
    
    /// <summary>
    /// Returns true if a component or tag was added / removed to / from the entity with the passed <paramref name="entityId"/>.
    /// </summary>
    /// <remarks>
    /// Therefore <see cref="EntityStore.EventRecorder"/> needs to be enabled and<br/> 
    /// the component / tag (add / remove) events of interest need to be added to the <see cref="EventFilter"/>.<br/>
    /// <br/>
    /// <b>Note</b>: <see cref="HasEvent"/> can be called from any thread.<br/>
    /// No structural changes like adding / removing components/tags must not be executed at the same time by another thread.
    /// </remarks>
    public bool HasEvent(int entityId)
    {
        if (eventFilter != null) {
            return eventFilter.HasEvent(entityId);
        }
        return false;
    }
    
    internal ArchetypeQuery(EntityStoreBase store, in SignatureIndexes indexes)
    {
        this.store          = store;
        archetypes          = Array.Empty<Archetype>();
        lastArchetypeCount  = 1;
        requiredComponents  = new ComponentTypes(indexes);
        signatureIndexes    = indexes;
    }
    
    internal ArchetypeQuery(EntityStoreBase store, in ComponentTypes componentTypes)
    {
        this.store          = store;
        archetypes          = Array.Empty<Archetype>();
        lastArchetypeCount  = 1;
        requiredComponents  = componentTypes;
    }
    
    /// <remarks>
    /// Reset <see cref="lastArchetypeCount"/> to force update of <see cref="archetypes"/> on subsequent call to <see cref="Archetypes"/>
    /// </remarks>
    private void Reset () {
        archetypes          = Array.Empty<Archetype>();
        lastArchetypeCount  = 1;
        archetypeCount      = 0;
    }
    
    // --- tags
    internal void SetHasAllTags(in Tags tags) {
        allTags         = tags;
        allTagsCount    = tags.Count;
        Reset();
    }
    
    internal void SetHasAnyTags(in Tags tags) {
        anyTags         = tags;
        anyTagsCount    = tags.Count;
        Reset();
    }
    
    internal void SetWithoutAllTags(in Tags tags) {
        withoutAllTags      = tags;
        withoutAllTagsCount = tags.Count;
        Reset();
    }
    
    internal void SetWithoutAnyTags(in Tags tags) {
        withoutAnyTags      = tags;
        Reset();
    }
    
    // --- components
    internal void SetHasAllComponents(in ComponentTypes types) {
        allComponents         = types;
        allComponentsCount    = types.Count;
        Reset();
    }
    
    internal void SetHasAnyComponents(in ComponentTypes types) {
        anyComponents         = types;
        anyComponentsCount    = types.Count;
        Reset();
    }
    
    internal void SetWithoutAllComponents(in ComponentTypes types) {
        withoutAllComponents      = types;
        withoutAllComponentsCount = types.Count;
        Reset();
    }
    
    internal void SetWithoutAnyComponents(in ComponentTypes types) {
        withoutAnyComponents      = types;
        Reset();
    }
    
    private ReadOnlySpan<Archetype> GetArchetypesSpan() {
        var archs = GetArchetypes();
        return new ReadOnlySpan<Archetype>(archs.array, 0, archs.length);
    }
    
    private bool IsTagsMatch(in Tags tags)
    {
        if (anyTagsCount > 0)
        {
            if (!tags.HasAny(anyTags))
            {
                if (allTagsCount == 0) {
                    return false;
                }
                if (!tags.HasAll(allTags)) {
                    return false;
                }
            }
        } else {
            if (!tags.HasAll(allTags)) {
                return false;
            }
        }
        if (tags.HasAny(withoutAnyTags)) {
            return false;
        }
        if (withoutAllTagsCount > 0 && tags.HasAll(withoutAllTags)) {
            return false;
        }
        return true;
    }
    
    private bool IsComponentsMatch(in ComponentTypes componentTypes)
    {
        if (anyComponentsCount > 0)
        {
            if (!componentTypes.HasAny(anyComponents))
            {
                if (allComponentsCount == 0) {
                    return false;
                }
                if (!componentTypes.HasAll(allComponents)) {
                    return false;
                }
            }
        } else {
            if (!componentTypes.HasAll(allComponents)) {
                return false;
            }
        }
        if (componentTypes.HasAny(withoutAnyComponents)) {
            return false;
        }
        if (withoutAllComponentsCount > 0 && componentTypes.HasAll(withoutAllComponents)) {
            return false;
        }
        return true;
    }
    
    internal Archetypes GetArchetypes()
    {
        if (store.ArchetypeCount == lastArchetypeCount) {
            return new Archetypes(archetypes, archetypeCount);
        }
        // --- update archetypes / archetypesCount: Add matching archetypes newly added to the store
        var storeArchetypes     = store.Archetypes;
        var newStoreLength      = storeArchetypes.Length;
        var nextArchetypes      = archetypes;
        var lastCount           = lastArchetypeCount;
        var nextCount           = archetypeCount;
        
        for (int n = lastCount; n < newStoreLength; n++)
        {
            var archetype = storeArchetypes[n];
            if (!archetype.componentTypes.HasAll(requiredComponents)) {
                continue;
            }
            if (!IsTagsMatch(archetype.tags)) {
                continue;
            }
            if (!IsComponentsMatch(archetype.componentTypes)) {
                continue;
            }
            if (nextCount == nextArchetypes.Length) {
                var length = Math.Max(4, 2 * nextCount);
                ArrayUtils.Resize(ref nextArchetypes, length);
            }
            nextArchetypes[nextCount++] = archetype;
        }
        // --- order matters in case of parallel execution
        archetypes          = nextArchetypes;   // using changed (added) archetypes with old archetypeCount         => OK
        archetypeCount      = nextCount;        // archetypes already changed                                       => OK
        lastArchetypeCount  = newStoreLength;   // using old lastArchetypeCount result only in a redundant update   => OK
        return new Archetypes(nextArchetypes, nextCount);
    }
    
    internal static ArgumentException ReadOnlyException(Type type) {
        return new ArgumentException($"Query does not contain Component type: {type.Name}");
    }
    
    internal string GetQueryChunksString() {
        return signatureIndexes.GetString($"QueryChunks[{ChunkCount}]  Components: ");
    }
    
    private string GetString() {
        var sb          = new StringBuilder();
        var hasTypes    = false;
        sb.Append("Query: [");
        var components  = EntityStoreBase.Static.EntitySchema.components;
        for (int n = 0; n < signatureIndexes.length; n++)
        {
            var structIndex = signatureIndexes.GetStructIndex(n);
            var structType  = components[structIndex];
            sb.Append(structType.Name);
            sb.Append(", ");
            hasTypes = true;
        }
        foreach (var tag in allTags) {
            sb.Append('#');
            sb.Append(tag.Name);
            sb.Append(", ");
            hasTypes = true;
        }
        if (hasTypes) {
            sb.Length -= 2;
            sb.Append(']');
        }
        sb.Append("  EntityCount: ");
        sb.Append(EntityCount);
        return sb.ToString();
    }
    
    private EventFilter GetEventFilter()
    {
        if (eventFilter != null) {
            return eventFilter;
        }
        return eventFilter = new EventFilter(Store.EventRecorder);
    }
    #endregion
}

public sealed class ArchetypeQuery<T1> : ArchetypeQuery
    where T1 : struct, IComponent
{
    [Browse(Never)] internal    T1[]    copyT1;
    
    public new ArchetypeQuery<T1> AllTags       (in Tags tags) { SetHasAllTags(tags);       return this; }
    public new ArchetypeQuery<T1> AnyTags       (in Tags tags) { SetHasAnyTags(tags);       return this; }
    public new ArchetypeQuery<T1> WithoutAllTags(in Tags tags) { SetWithoutAllTags(tags);   return this; }
    public new ArchetypeQuery<T1> WithoutAnyTags(in Tags tags) { SetWithoutAnyTags(tags);   return this; }
    
    public new ArchetypeQuery<T1> AllComponents       (in ComponentTypes componentTypes) { SetHasAllComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1> AnyComponents       (in ComponentTypes componentTypes) { SetHasAnyComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1> WithoutAllComponents(in ComponentTypes componentTypes) { SetWithoutAllComponents(componentTypes);   return this; }
    public new ArchetypeQuery<T1> WithoutAnyComponents(in ComponentTypes componentTypes) { SetWithoutAnyComponents(componentTypes);   return this; }
    
    internal ArchetypeQuery(EntityStoreBase store, in Signature<T1> signature)
        : base(store, signature.signatureIndexes) {
    }
    
    public ArchetypeQuery<T1> ReadOnly<T>()
        where T : struct, IComponent
    {
        if (typeof(T1) == typeof(T)) { copyT1 = new T1[ChunkSize]; return this; }
        throw ReadOnlyException(typeof(T));
    }
    
    /// <summary> Return the <see cref="Chunk{T}"/>'s storing the components and entities of an <see cref="ArchetypeQuery{T1}"/>. </summary> 
    public      QueryChunks <T1>  Chunks                                      => new (this);
}

public sealed class ArchetypeQuery<T1, T2> : ArchetypeQuery // : IEnumerable <>  // <- not implemented to avoid boxing
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    [Browse(Never)] internal    T1[]    copyT1;
    [Browse(Never)] internal    T2[]    copyT2;
    
     public new ArchetypeQuery<T1, T2> AllTags       (in Tags tags) { SetHasAllTags(tags);      return this; }
     public new ArchetypeQuery<T1, T2> AnyTags       (in Tags tags) { SetHasAnyTags(tags);      return this; }
     public new ArchetypeQuery<T1, T2> WithoutAllTags(in Tags tags) { SetWithoutAllTags(tags);  return this; }
     public new ArchetypeQuery<T1, T2> WithoutAnyTags(in Tags tags) { SetWithoutAnyTags(tags);  return this; }
     
     public new ArchetypeQuery<T1, T2> AllComponents       (in ComponentTypes componentTypes) { SetHasAllComponents(componentTypes);       return this; }
     public new ArchetypeQuery<T1, T2> AnyComponents       (in ComponentTypes componentTypes) { SetHasAnyComponents(componentTypes);       return this; }
     public new ArchetypeQuery<T1, T2> WithoutAllComponents(in ComponentTypes componentTypes) { SetWithoutAllComponents(componentTypes);   return this; }
     public new ArchetypeQuery<T1, T2> WithoutAnyComponents(in ComponentTypes componentTypes) { SetWithoutAnyComponents(componentTypes);   return this; }
    
    internal ArchetypeQuery(EntityStoreBase store, in Signature<T1, T2> signature)
        : base(store, signature.signatureIndexes) {
    }
    
    public ArchetypeQuery<T1, T2> ReadOnly<T>()
        where T : struct, IComponent
    {
        if (typeof(T1) == typeof(T)) { copyT1 = new T1[ChunkSize]; return this; }
        if (typeof(T2) == typeof(T)) { copyT2 = new T2[ChunkSize]; return this; }
        throw ReadOnlyException(typeof(T));
    }
    
    /// <summary> Return the <see cref="Chunk{T}"/>'s storing the components and entities of an <see cref="ArchetypeQuery{T1,T2}"/>. </summary> 
    public      QueryChunks    <T1,T2>  Chunks                                      => new (this);
}

public sealed class ArchetypeQuery<T1, T2, T3> : ArchetypeQuery
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
{
    [Browse(Never)] internal    T1[]    copyT1;
    [Browse(Never)] internal    T2[]    copyT2;
    [Browse(Never)] internal    T3[]    copyT3;
    
    public new ArchetypeQuery<T1, T2, T3> AllTags       (in Tags tags) { SetHasAllTags(tags);       return this; }
    public new ArchetypeQuery<T1, T2, T3> AnyTags       (in Tags tags) { SetHasAnyTags(tags);       return this; }
    public new ArchetypeQuery<T1, T2, T3> WithoutAllTags(in Tags tags) { SetWithoutAllTags(tags);   return this; }
    public new ArchetypeQuery<T1, T2, T3> WithoutAnyTags(in Tags tags) { SetWithoutAnyTags(tags);   return this; }
    
    public new ArchetypeQuery<T1, T2, T3> AllComponents       (in ComponentTypes componentTypes) { SetHasAllComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1, T2, T3> AnyComponents       (in ComponentTypes componentTypes) { SetHasAnyComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1, T2, T3> WithoutAllComponents(in ComponentTypes componentTypes) { SetWithoutAllComponents(componentTypes);   return this; }
    public new ArchetypeQuery<T1, T2, T3> WithoutAnyComponents(in ComponentTypes componentTypes) { SetWithoutAnyComponents(componentTypes);   return this; }
    
    internal ArchetypeQuery(EntityStoreBase store, in Signature<T1, T2, T3> signature)
        : base(store, signature.signatureIndexes) {
    }
    
    public ArchetypeQuery<T1, T2, T3> ReadOnly<T>()
        where T : struct, IComponent
    {
        if (typeof(T1) == typeof(T)) { copyT1 = new T1[ChunkSize]; return this; }
        if (typeof(T2) == typeof(T)) { copyT2 = new T2[ChunkSize]; return this; }
        if (typeof(T3) == typeof(T)) { copyT3 = new T3[ChunkSize]; return this; }
        throw ReadOnlyException(typeof(T));
    }
    
    /// <summary> Return the <see cref="Chunk{T}"/>'s storing the components and entities of an <see cref="ArchetypeQuery{T1,T2,T3}"/>. </summary>
    public      QueryChunks    <T1, T2, T3>  Chunks         => new (this);
}

public sealed class ArchetypeQuery<T1, T2, T3, T4> : ArchetypeQuery
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
{
    [Browse(Never)] internal    T1[]    copyT1;
    [Browse(Never)] internal    T2[]    copyT2;
    [Browse(Never)] internal    T3[]    copyT3;
    [Browse(Never)] internal    T4[]    copyT4;
    
    public new ArchetypeQuery<T1, T2, T3, T4> AllTags       (in Tags tags) { SetHasAllTags(tags);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4> AnyTags       (in Tags tags) { SetHasAnyTags(tags);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4> WithoutAllTags(in Tags tags) { SetWithoutAllTags(tags);   return this; }
    public new ArchetypeQuery<T1, T2, T3, T4> WithoutAnyTags(in Tags tags) { SetWithoutAnyTags(tags);   return this; }
    
    public new ArchetypeQuery<T1, T2, T3, T4> AllComponents       (in ComponentTypes componentTypes) { SetHasAllComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4> AnyComponents       (in ComponentTypes componentTypes) { SetHasAnyComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4> WithoutAllComponents(in ComponentTypes componentTypes) { SetWithoutAllComponents(componentTypes);   return this; }
    public new ArchetypeQuery<T1, T2, T3, T4> WithoutAnyComponents(in ComponentTypes componentTypes) { SetWithoutAnyComponents(componentTypes);   return this; }
    
    internal ArchetypeQuery(EntityStoreBase store, in Signature<T1, T2, T3, T4> signature)
        : base(store, signature.signatureIndexes) {
    }
    
    public ArchetypeQuery<T1, T2, T3, T4> ReadOnly<T>()
        where T : struct, IComponent
    {
        if (typeof(T1) == typeof(T)) { copyT1 = new T1[ChunkSize]; return this; }
        if (typeof(T2) == typeof(T)) { copyT2 = new T2[ChunkSize]; return this; }
        if (typeof(T3) == typeof(T)) { copyT3 = new T3[ChunkSize]; return this; }
        if (typeof(T4) == typeof(T)) { copyT4 = new T4[ChunkSize]; return this; }
        throw ReadOnlyException(typeof(T));
    }
    
    /// <summary> Return the <see cref="Chunk{T}"/>'s storing the components and entities of an <see cref="ArchetypeQuery{T1,T2,T3,T4}"/>. </summary>
    public      QueryChunks    <T1, T2, T3, T4>  Chunks         => new (this);
}

public sealed class ArchetypeQuery<T1, T2, T3, T4, T5> : ArchetypeQuery
    where T1 : struct, IComponent
    where T2 : struct, IComponent
    where T3 : struct, IComponent
    where T4 : struct, IComponent
    where T5 : struct, IComponent
{
    [Browse(Never)] internal    T1[]    copyT1;
    [Browse(Never)] internal    T2[]    copyT2;
    [Browse(Never)] internal    T3[]    copyT3;
    [Browse(Never)] internal    T4[]    copyT4;
    [Browse(Never)] internal    T5[]    copyT5;
    
    public new ArchetypeQuery<T1, T2, T3, T4, T5> AllTags       (in Tags tags) { SetHasAllTags(tags);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4, T5> AnyTags       (in Tags tags) { SetHasAnyTags(tags);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4, T5> WithoutAllTags(in Tags tags) { SetWithoutAllTags(tags);   return this; }
    public new ArchetypeQuery<T1, T2, T3, T4, T5> WithoutAnyTags(in Tags tags) { SetWithoutAnyTags(tags);   return this; }
    
    public new ArchetypeQuery<T1, T2, T3, T4, T5> AllComponents       (in ComponentTypes componentTypes) { SetHasAllComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4, T5> AnyComponents       (in ComponentTypes componentTypes) { SetHasAnyComponents(componentTypes);       return this; }
    public new ArchetypeQuery<T1, T2, T3, T4, T5> WithoutAllComponents(in ComponentTypes componentTypes) { SetWithoutAllComponents(componentTypes);   return this; }
    public new ArchetypeQuery<T1, T2, T3, T4, T5> WithoutAnyComponents(in ComponentTypes componentTypes) { SetWithoutAnyComponents(componentTypes);   return this; }
    
    internal ArchetypeQuery(EntityStoreBase store, in Signature<T1, T2, T3, T4, T5> signature)
        : base(store, signature.signatureIndexes) {
    }
    
    public ArchetypeQuery<T1, T2, T3, T4, T5> ReadOnly<T>()
        where T : struct, IComponent
    {
        if (typeof(T1) == typeof(T)) { copyT1 = new T1[ChunkSize]; return this; }
        if (typeof(T2) == typeof(T)) { copyT2 = new T2[ChunkSize]; return this; }
        if (typeof(T3) == typeof(T)) { copyT3 = new T3[ChunkSize]; return this; }
        if (typeof(T4) == typeof(T)) { copyT4 = new T4[ChunkSize]; return this; }
        if (typeof(T5) == typeof(T)) { copyT5 = new T5[ChunkSize]; return this; }
        throw ReadOnlyException(typeof(T));
    }
    
    /// <summary> Return the <see cref="Chunk{T}"/>'s storing the components and entities of an <see cref="ArchetypeQuery{T1,T2,T3,T4,T5}"/>. </summary>
    public      QueryChunks    <T1, T2, T3, T4, T5>  Chunks         => new (this);
}

internal static class EnumeratorUtils
{
    [Conditional("DEBUG")] [ExcludeFromCodeCoverage]
    internal static void AssertComponentLenGreater0 (int componentLen) {
        if (componentLen <= 0) throw new InvalidOperationException("expect componentLen > 0");
    }
}