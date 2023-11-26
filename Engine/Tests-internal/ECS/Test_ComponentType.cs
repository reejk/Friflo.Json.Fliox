﻿using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Internal.ECS;

public struct InternalTestTag  : IEntityTag { }

[ExcludeFromCodeCoverage]
public static class Test_ComponentType
{
    [Test]
    public static void Test_ComponentSchema_Dependencies()
    {
        ComponentSchema schema = EntityStore.GetComponentSchema();
        AreEqual(4, schema.EngineDependants.Length);
        
        
        var e = Throws<InvalidOperationException>(() =>
        {
            schema.CheckStructIndex(typeof(string), schema.maxStructIndex);    
        });
        var expect = $"number of structs exceed EntityStore.maxStructIndex: {schema.maxStructIndex}";
        AreEqual(expect, e!.Message);
    }
    
    [Test]
    public static void Test_ComponentType_Exceptions()
    {
        var componentType = new TagType(typeof(string), 0);
        var e = Throws<InvalidOperationException>(() => {
            componentType.ReadScript(null, default, null);
        });
        AreEqual("operates only on ScriptType<>", e!.Message);
        
        e = Throws<InvalidOperationException>(() => {
            componentType.CreateHeap();
        });
        AreEqual("operates only on StructComponentType<>", e!.Message);
    }
    
    [Test]
    public static void Test_ComponentType_MissingAttribute()
    {
        var e = Throws<InvalidOperationException>(() => {
            SchemaUtils.CreateSchemaType(typeof(string), null);
        });
        AreEqual("missing expected attribute. Type: System.String", e!.Message);
    }

}
