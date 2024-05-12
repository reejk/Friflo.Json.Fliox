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
}

}
