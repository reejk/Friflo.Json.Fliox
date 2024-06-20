﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Index;

internal readonly struct IndexedComponentType
{
    internal readonly ComponentType componentType;
    private  readonly MethodInfo    createIndex;
    
    private IndexedComponentType(ComponentType componentType, MethodInfo createIndex) {
        this.componentType  = componentType;
        this.createIndex    = createIndex;
    }
    
    internal ComponentIndex CreateComponentIndex(EntityStore store, ComponentType componentType)
    {
        var obj     = createIndex.Invoke(null, new object[] { componentType });
        var index   = (ComponentIndex)obj!;
        index.store = store;
        return index;
    }
    
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2080", Justification = "TODO")] // TODO
    internal static void AddIndexedComponentType(SchemaTypes schemaTypes, ComponentType componentType)
    {
        var interfaces = componentType.Type.GetInterfaces();
        foreach (var i in interfaces)
        {
            if (!i.IsGenericType) continue;
            var genericType = i.GetGenericTypeDefinition();
            if (genericType != typeof(IIndexedComponent<>)) {
                continue;
            }
            var valueType               = i.GenericTypeArguments[0];
            var createIndex             = MakeCreateIndex(valueType);
            var indexedComponentType    = new IndexedComponentType(componentType, createIndex);
            schemaTypes.indexedComponents.Add(indexedComponentType);
        }
    }
    
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = "TODO")] // TODO
    private static MethodInfo MakeCreateIndex(Type valueType)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
        var method = typeof(IndexedComponentType).GetMethod(nameof(CreateInvertedIndex), flags);
        return method?.MakeGenericMethod(valueType);
    }
    
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = "TODO")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055", Justification = "TODO")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "TODO")]
    internal static ComponentIndex CreateInvertedIndex<TValue>(ComponentType componentType)
    {
        var indexType = ComponentIndexAttribute.GetComponentIndex(componentType.Type);
        if (indexType != null) {
            var genericType = indexType.MakeGenericType(new Type[] { typeof(TValue) });
            return(ComponentIndex)Activator.CreateInstance(genericType);
        }
        if (typeof(TValue) == typeof(Entity)) {
            return new HasEntityIndex();    
        }
        return new HasValueIndex<TValue>();
    }
}

