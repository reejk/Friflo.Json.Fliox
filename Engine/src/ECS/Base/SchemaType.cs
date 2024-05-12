// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using static Friflo.Engine.ECS.SchemaTypeKind;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Provide meta data for <see cref="IComponent"/> / <see cref="ITag"/> structs. 
/// </summary>
public abstract class SchemaType
{
    /// <summary>Returns the <see cref="SchemaTypeKind"/> of the type.</summary>
    /// <returns>
    /// <see cref="Component"/> if the type is a <see cref="IComponent"/><br/>
    /// <see cref="Tag"/> if the type is an <see cref="ITag"/><br/>
    /// </returns>
    public   readonly   SchemaTypeKind  Kind;               //  1
    
    /// <summary>
    /// If <see cref="Kind"/> == <see cref="Tag"/> the type of a <b>tag</b> struct implementing <see cref="ITag"/>.<br/>
    /// If <see cref="Kind"/> == <see cref="Component"/> the type of a <b>component</b> struct implementing <see cref="IComponent"/>.
    /// </summary>
    public   readonly   Type            Type;               //  8
    
    /// <summary>Returns the <see cref="System.Type"/> name of the struct / class. </summary>
    public   readonly   string          Name;               //  8
        
    internal SchemaType(Type type, SchemaTypeKind kind)
    {
        Kind            = kind;
        Type            = type;
        Name            = type.Name;
    }
}
