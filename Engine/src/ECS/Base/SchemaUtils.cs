// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable UseCollectionExpression
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal sealed class SchemaTypes
{
    internal readonly   List<ComponentType> components  = new ();
    internal readonly   List<TagType>       tags        = new ();
}

internal static class SchemaUtils
{
    internal static EntitySchema RegisterSchemaTypes()
    {
        var assemblyLoader  = new AssemblyLoader();
        var assemblies      = assemblyLoader.GetEngineDependants();
        
        var dependants  = assemblyLoader.dependants;
        var schemaTypes = new SchemaTypes();
        foreach (var assembly in assemblies) {
            var types           = AssemblyLoader.GetComponentTypes(assembly);
            var engineTypes     = new List<SchemaType>();
            foreach (var type in types) {
                var schemaType = CreateSchemaType(type, schemaTypes);
                engineTypes.Add(schemaType);
            }
            dependants.Add(new EngineDependant (assembly, engineTypes));
        }
        Console.WriteLine(assemblyLoader);
        return new EntitySchema(dependants, schemaTypes);
    }
    
    internal static SchemaType CreateSchemaType(Type type, SchemaTypes schemaTypes)
    {
        const BindingFlags flags    = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
        
        if (type.IsValueType) {
            if (typeof(ITag).IsAssignableFrom(type))
            {
                // type: ITag
                var tagIndex        = schemaTypes.tags.Count + 1;
                var createParams    = new object[] { tagIndex };
                var method          = typeof(SchemaUtils).GetMethod(nameof(CreateTagType), flags);
                var genericMethod   = method!.MakeGenericMethod(type);
                var tagType         = (TagType)genericMethod.Invoke(null, createParams);
                schemaTypes.tags.Add(tagType);
                return tagType;
            }
            if (typeof(IComponent).IsAssignableFrom(type))
            {
                // type: IComponent
                var structIndex     = schemaTypes.components.Count + 1;
                var createParams    = new object[] { structIndex };
                var method          = typeof(SchemaUtils).GetMethod(nameof(CreateComponentType), flags);
                var genericMethod   = method!.MakeGenericMethod(type);
                var componentType   = (ComponentType)genericMethod.Invoke(null, createParams);
                schemaTypes.components.Add(componentType);
                return componentType;
            }
        }
        throw new InvalidOperationException($"Cannot create SchemaType for Type: {type}");
    }
    
    internal static ComponentType CreateComponentType<T>(int structIndex)
        where T : struct, IComponent
    {
        return new ComponentType<T>(structIndex);
    }
    
    /// <remarks>
    /// <see cref="TagType{T}.TagIndex"/> must be assigned here.<br/>
    /// Unity initializes static fields of generic types already when creating a instance of that type.  
    /// </remarks>
    internal static TagType CreateTagType<T>(int tagIndex)
        where T : struct, ITag
    {
        return new TagType(typeof(T), tagIndex);
    }
}
