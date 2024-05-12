using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable InconsistentNaming
namespace Internal.ECS {

public static class Test_SchemaType
{
    /// <summary>
    /// Ensure initialization of <see cref="ScriptType{T}.Index"/>.
    /// Especially if <see cref="Tags.Get{T}"/> is the first call in an application.  
    /// </summary>
    [Test]
    public static void Test_SchemaType_Script_Index()
    {
        var scriptIndex = ScriptType<TestScript1>.Index;
        var schema      = EntityStore.GetEntitySchema();
        var scriptType   = schema.scripts[scriptIndex];
        
        AreEqual("TestScript1",         scriptType.Name);
        AreEqual(scriptIndex,           scriptType.ScriptIndex);
        AreEqual(typeof(TestScript1),   scriptType.Type);
    }

    /// <summary>
    /// Ensure initialization of <see cref="TagType{T}.TagIndex"/>.
    /// </summary>
    [Test]
    public static void Test_SchemaType_Tag_Index()
    {
        var tagIndex    = TagType<TestTag>.TagIndex;
        var schema      = EntityStore.GetEntitySchema();
        var tagType     = schema.tags[tagIndex];
        
        AreEqual("TestTag",         tagType.Name);
        AreEqual(tagIndex,          tagType.TagIndex);
        AreEqual(typeof(TestTag),   tagType.Type);
    }
    
    /// <summary>
    /// Ensure initialization of <see cref="StructHeap{T}.StructIndex"/>.
    /// </summary>
    [Test]
    public static void Test_SchemaType_StructIndex()
    {
        int count = 0;
        var componentTypes = ComponentTypes.Get<Position>();
        foreach (var type in componentTypes) {
            count++;
            AreEqual("Position",        type.Name);
            AreEqual(typeof(Position),  type.Type);
        }
        AreEqual(1, count);
    }
        
    [Test]
    public static void Test_SchemaType_Tags_Get_Perf()
    {
        var count   = 10; // 10_000_000_000 ~ #PC: 2499 ms
        var sw = new Stopwatch();
        sw.Start();
        for (long n = 0; n < count; n++) {
            Tags.Get<TestTag>();
        }
        Console.WriteLine($"Tags.Get<>() - duration: {sw.ElapsedMilliseconds} ms");
    }
    
    [Test]
    public static void Test_SchemaType_ComponentTypes_Get_Perf()
    {
        var count   = 10; // 10_000_000_000 ~ #PC: 2544 ms
        var sw = new Stopwatch();
        sw.Start();
        for (long n = 0; n < count; n++) {
            ComponentTypes.Get<MyComponent1>();
        }
        Console.WriteLine($"ComponentTypes.Get<>() - duration: {sw.ElapsedMilliseconds} ms");
    }
}

}
