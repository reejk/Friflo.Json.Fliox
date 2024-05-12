using Friflo.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.Base {

public static class Test_ComponentSchema
{
    [Test]
    public static void Test_EntityTags() {
        var schema      = EntityStore.GetEntitySchema();
        AreEqual(11,     schema.Tags.Length);
        
        var tags = schema.Tags;
        IsNull(tags[0]);
        for (int n = 1; n < tags.Length; n++) {
            var type = tags[n];
            AreEqual(n,                 type.TagIndex);
            AreEqual(SchemaTypeKind.Tag, type.Kind);
        }
        AreEqual(10,                     schema.TagTypeByType.Count);
        {
            var testTagType = schema.TagTypeByType[typeof(TestTag)];
            AreEqual(typeof(TestTag),       testTagType.Type);
            AreEqual("tag: [#TestTag]",     testTagType.ToString());
        } {
            var testTagType = schema.GetTagType<TestTag>();
            AreEqual(typeof(TestTag),       testTagType.Type);
            AreEqual("tag: [#TestTag]",     testTagType.ToString());
        } 
    }
    
    [Test]
    public static void Test_ComponentTypes()
    {
        var schema      = EntityStore.GetEntitySchema();
        var components  = schema.Components;
        
        AreEqual("components: 37  entity tags: 10", schema.ToString());
        AreEqual(38,    components.Length);
        AreEqual(37,    schema.ComponentTypeByType.Count);
        
        IsNull(components[0]);
        for (int n = 1; n < components.Length; n++) {
            var type = components[n];
            AreEqual(n, type.StructIndex);
            AreEqual(SchemaTypeKind.Component, type.Kind);
        }
        {
            var componentType = schema.GetComponentType<MyComponent1>();
            AreEqual("Component: [MyComponent1]",       componentType.ToString());
        }
    }
}

}