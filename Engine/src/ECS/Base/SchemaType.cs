﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using static Friflo.Engine.ECS.SchemaTypeKind;

// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal enum BlittableType
{
    Blittable,
    NonBlittable,
    Unknown,
}

/// <summary>
/// Provide meta data for <see cref="Script"/> classes and <see cref="IComponent"/> / <see cref="ITag"/> structs. 
/// </summary>
public abstract class SchemaType
{
    /// <summary>
    /// If <see cref="Kind"/> is a <see cref="Component"/> or a <see cref="Script"/> the key assigned
    /// with <see cref="ComponentKeyAttribute"/>.<br/>
    /// If null the component is not serialized.
    /// </summary>
    public   readonly   string          ComponentKey;       //  8
    
    /// <summary>Returns the <see cref="SchemaTypeKind"/> of the type.</summary>
    /// <returns>
    /// <see cref="Script"/> if the type is a <see cref="Script"/><br/>
    /// <see cref="Component"/> if the type is a <see cref="IComponent"/><br/>
    /// <see cref="Tag"/> if the type is an <see cref="ITag"/><br/>
    /// </returns>
    public   readonly   SchemaTypeKind  Kind;               //  1
    
    /// <summary>
    /// If <see cref="Kind"/> == <see cref="Tag"/> the type of a <b>tag</b> struct implementing <see cref="ITag"/>.<br/>
    /// If <see cref="Kind"/> == <see cref="Component"/> the type of a <b>component</b> struct implementing <see cref="IComponent"/>.<br/>
    /// If <see cref="Kind"/> == <see cref="Script"/> the type of a <b>script</b> class extending <see cref="Script"/>.<br/>
    /// </summary>
    public   readonly   Type            Type;               //  8
    
    /// <summary>Returns the <see cref="System.Type"/> name of the struct / class. </summary>
    public   readonly   string          Name;               //  8
    
    /// <summary>
    /// A string with 1, 2 or 3 characters used to to symbolize a component, tag or script in a UI or console.<br/>
    /// See <see cref="ComponentSymbolAttribute"/>
    /// </summary>
    public   readonly   string          SymbolName;         //  8
    
    /// <summary>
    /// A color used to to symbolize a component, tag or script in a UI. <br/>
    /// See <see cref="ComponentSymbolAttribute"/>
    /// </summary>
    public   readonly   SymbolColor?    SymbolColor;        // 12  
        
    internal SchemaType(string componentKey, Type type, SchemaTypeKind kind)
    {
        ComponentKey    = componentKey;
        Kind            = kind;
        Type            = type;
        Name            = type.Name;
        SchemaUtils.GetComponentSymbol(type, out SymbolName, out SymbolColor);
    }
    
    private static readonly Dictionary<Type, BlittableType> BlittableTypes = new Dictionary<Type, BlittableType>();
    
    static SchemaType()
    {
        var types = BlittableTypes;
        var blittable = BlittableType.Blittable;
        types.Add(typeof(bool),         blittable);
        types.Add(typeof(char),         blittable);
        types.Add(typeof(decimal),      blittable);
        //
        types.Add(typeof(byte),         blittable);
        types.Add(typeof(short),        blittable);
        types.Add(typeof(int),          blittable);
        types.Add(typeof(long),         blittable);
        //
        types.Add(typeof(sbyte),        blittable);
        types.Add(typeof(ushort),       blittable);
        types.Add(typeof(uint),         blittable);
        types.Add(typeof(ulong),        blittable);
        //
        types.Add(typeof(float),        blittable);
        types.Add(typeof(double),       blittable);
        //
        types.Add(typeof(Guid),         blittable);
        types.Add(typeof(DateTime),     blittable);
        types.Add(typeof(BigInteger),   blittable);
        //
        types.Add(typeof(Entity),       blittable);
        //
        types.Add(typeof(string),       blittable);
    }
    
    // todo - add test assertion EntityName is a blittable type 
    internal static BlittableType GetBlittableType(Type type)
    {
        // if (type.Name == "CycleClass") { _ = 42; }
        if (BlittableTypes.TryGetValue(type, out BlittableType blittable)) {
            return blittable;
        }
        if (type.IsArray) {
            blittable = BlittableType.NonBlittable;    
        } else if (type.IsClass || type.IsValueType) {
            // detect cycle in class type hierarchy by adding a temporary unknown
            BlittableTypes.Add(type, BlittableType.Unknown);
            blittable = AreAllMembersBlittable(type);
        }
        BlittableTypes[type] = blittable;
        return blittable;
    }
    
    private const BindingFlags MemberFlags =
        BindingFlags.Public             |
        BindingFlags.NonPublic          |
        BindingFlags.Instance           |
        BindingFlags.FlattenHierarchy;

    private static BlittableType AreAllMembersBlittable(Type type)
    {
        var members = type.GetMembers(MemberFlags);
        foreach (var member in members)
        {
            switch (member) {
                case FieldInfo fieldInfo:
                    var fieldType = fieldInfo.FieldType;
                    if (GetBlittableType(fieldType) == BlittableType.Blittable) {
                        continue;
                    }
                    return BlittableType.NonBlittable;
                case PropertyInfo: // propertyInfo:
                    continue;
                /*  var propertyType = propertyInfo.PropertyType;
                    if (IsBlittableType(propertyType)) {
                        continue;
                    }
                    return false; */
            }
        }
        return BlittableType.Blittable;
    }
}

public readonly struct SymbolColor
{
    public readonly byte r;
    public readonly byte g;
    public readonly byte b;
    
    public SymbolColor (byte r, byte g, byte b) {
        this.r = r;
        this.g = g;
        this.b = b;
    }
}
