﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using static Friflo.Fliox.Engine.ECS.EntityStore.Static;

// ReSharper disable ArrangeTrailingCommaInMultilineLists
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

public sealed partial class EntityStore
{
    // -------------------------------------- get archetype --------------------------------------
#region get archetype
    private Archetype GetArchetype<T>(in Tags tags)
        where T : struct, IStructComponent
    {
        searchKey.SetTagsWith(tags, StructHeap<T>.StructIndex);
        if (archetypeSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config  = GetArchetypeConfig();
        var schema  = Static.ComponentSchema;
        var types   = new SignatureTypeSet(1,
            T1: schema.GetStructType(StructHeap<T>.StructIndex, typeof(T))
        );
        var archetype = Archetype.CreateWithSignatureTypes(config, types, tags);
        AddArchetype(archetype);
        return archetype;
    }
    
    internal ArchetypeConfig GetArchetypeConfig() {
        return new ArchetypeConfig (this, archetypesCount, DefaultCapacity);
    }
    
    private Archetype GetArchetypeWithSignature(in ArchetypeStructs structs, in SignatureTypeSet types, in Tags tags)
    {
        searchKey.SetMaskTags(structs, tags);
        if (archetypeSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config      = GetArchetypeConfig();
        var archetype   = Archetype.CreateWithSignatureTypes(config, types, tags);
        AddArchetype(archetype);
        return archetype;
    }
    
    public Archetype FindArchetype(in ArchetypeStructs structs, in Tags tags) {
        searchKey.structs   = structs;
        searchKey.tags      = tags;
        searchKey.CalculateHashCode();
        archetypeSet.TryGetValue(searchKey, out var actualValue);
        return actualValue?.archetype;
    }
    
    public Archetype GetArchetype(in Tags tags)
    {
        return GetArchetypeWithSignature(default, default, tags);
    }

    public Archetype GetArchetype<T>(in Signature<T> signature, in Tags tags = default)
        where T : struct, IStructComponent
    {
        return GetArchetypeWithSignature(signature.structs, signature.types, tags);
    }
    
    public Archetype GetArchetype<T1, T2>(in Signature<T1, T2> signature, in Tags tags = default)
        where T1 : struct, IStructComponent
        where T2 : struct, IStructComponent
    {
        return GetArchetypeWithSignature(signature.structs, signature.types, tags);
    }
    
    public Archetype GetArchetype<T1, T2, T3>(in Signature<T1, T2, T3> signature, in Tags tags = default)
        where T1 : struct, IStructComponent
        where T2 : struct, IStructComponent
        where T3 : struct, IStructComponent
    {
        return GetArchetypeWithSignature(signature.structs, signature.types, tags);
    }
    
    public Archetype GetArchetype<T1, T2, T3, T4>(in Signature<T1, T2, T3, T4> signature, in Tags tags = default)
        where T1 : struct, IStructComponent
        where T2 : struct, IStructComponent
        where T3 : struct, IStructComponent
        where T4 : struct, IStructComponent
    {
        return GetArchetypeWithSignature(signature.structs, signature.types, tags);
    }
    
    public Archetype GetArchetype<T1, T2, T3, T4, T5>(in Signature<T1, T2, T3, T4, T5> signature, in Tags tags = default)
        where T1 : struct, IStructComponent
        where T2 : struct, IStructComponent
        where T3 : struct, IStructComponent
        where T4 : struct, IStructComponent
        where T5 : struct, IStructComponent
    {
        return GetArchetypeWithSignature(signature.structs, signature.types, tags);
    }
    #endregion
    
    // -------------------------------------- archetype query --------------------------------------
#region archetype query
    /// <summary>
    /// Create a reusable <see cref="ArchetypeQuery"/> for the <see cref="EntityStore"/>
    /// </summary>
    public ArchetypeQuery Query ()
    {
        return new ArchetypeQuery(this, default, default);
    }

    /// <summary>
    /// Create a reusable <see cref="ArchetypeQuery"/> for the <see cref="EntityStore"/>
    /// </summary>
    public ArchetypeQuery<T> Query<T> (in Signature<T> signature)
        where T : struct, IStructComponent
    {
        return new ArchetypeQuery<T>(this, signature);
    }
    
    public ArchetypeQuery<T> Query<T> ()
        where T : struct, IStructComponent
    {
        return new ArchetypeQuery<T>(this, Signature.Get<T>());
    }
    
    /// <summary>
    /// Create a reusable <see cref="ArchetypeQuery"/> for the <see cref="EntityStore"/>
    /// </summary>
    public ArchetypeQuery<T1, T2> Query<T1, T2> (in Signature<T1, T2> signature)
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2>(this, signature);
    }
    
    public ArchetypeQuery<T1, T2> Query<T1, T2> ()
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2>(this, Signature.Get<T1, T2>());
    }
    
    /// <summary>
    /// Create a reusable <see cref="ArchetypeQuery"/> for the <see cref="EntityStore"/>
    /// </summary>
    public ArchetypeQuery<T1, T2, T3> Query<T1, T2, T3> (in Signature<T1, T2, T3> signature)
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
        where T3: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2, T3>(this, signature);
    }
    
    public ArchetypeQuery<T1, T2, T3> Query<T1, T2, T3> ()
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
        where T3: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2, T3>(this, Signature.Get<T1, T2, T3>());
    }
    
    /// <summary>
    /// Create a reusable <see cref="ArchetypeQuery"/> for the <see cref="EntityStore"/>
    /// </summary>
    public ArchetypeQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4> (in Signature<T1, T2, T3, T4> signature)
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
        where T3: struct, IStructComponent
        where T4: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2, T3, T4>(this, signature);
    }
    
    public ArchetypeQuery<T1, T2, T3, T4> Query<T1, T2, T3, T4> ()
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
        where T3: struct, IStructComponent
        where T4: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2, T3, T4>(this, Signature.Get<T1, T2, T3, T4>());
    }
    
    /// <summary>
    /// Create a reusable <see cref="ArchetypeQuery"/> for the <see cref="EntityStore"/>
    /// </summary>
    public ArchetypeQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5> (in Signature<T1, T2, T3, T4, T5> signature)
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
        where T3: struct, IStructComponent
        where T4: struct, IStructComponent
        where T5: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2, T3, T4, T5>(this, signature);
    }
    
    public ArchetypeQuery<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5> ()
        where T1: struct, IStructComponent
        where T2: struct, IStructComponent
        where T3: struct, IStructComponent
        where T4: struct, IStructComponent
        where T5: struct, IStructComponent
    {
        return new ArchetypeQuery<T1, T2, T3, T4, T5>(this, Signature.Get<T1, T2, T3, T4, T5>());
    }
    
    #endregion
}
