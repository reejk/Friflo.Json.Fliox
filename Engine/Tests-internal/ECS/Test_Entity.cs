using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable InconsistentNaming
namespace Internal.ECS;

public static class Test_Entity
{
    [Test]
    public static void Test_Entity_Components()
    {
        var store = new EntityStore();
        var entity = store.CreateEntity();
        var components = entity.Components.GetComponentArray(); 
        AreEqual(0, components.Length);
        
        entity.AddComponent(new Position(1, 2, 3));
        entity.AddComponent(new EntityName("test"));
       
        components = entity.Components.GetComponentArray();
        AreEqual(2, components.Length);
        AreEqual("test",                ((EntityName)components[0]).value);
        AreEqual(new Position(1,2,3),   (Position)components[1]);
    }
    
    
    [Test]
    public static void Test_Entity_Children()
    {
        var store = new EntityStore();
        var entity = store.CreateEntity();
        var child1 = store.CreateEntity();
        var child2 = store.CreateEntity();
        var sub11   = store.CreateEntity();
        var sub12   = store.CreateEntity();
        var sub21   = store.CreateEntity();
        
        AreEqual(0, entity.ChildEntities.ToArray().Length);
        
        entity.AddChild(child1);
        entity.AddChild(child2);
        child1.AddChild(sub11);
        child1.AddChild(sub12);
        child2.AddChild(sub21);
        
        var children = entity.ChildEntities.ToArray();
        AreEqual(2, children.Length);
        AreEqual(child1, children[0]);
        AreEqual(child2, children[1]);
        
        AreEqual(2, child1.ChildEntities.Count);
        AreEqual(1, child2.ChildEntities.Count);
    }
    
    [Test]
    public static void Test_Entity_Info()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var entity  = store.CreateEntity();
        
        entity.AddComponent<Position>();
        entity.AddScript(new TestScript1());
        entity.AddChild(store.CreateEntity());
        entity.AddChild(store.CreateEntity());
        
        var json =
"""
{
    "id": 1,
    "children": [
        2,
        3
    ],
    "components": {
        "pos": {"x":0,"y":0,"z":0},
        "script1": {"val1":0}
    }
}
""";
        AreEqual("",                            entity.Info.ToString());
        AreEqual(entity.Pid,                    entity.Info.Pid);
        AreEqual(json,                          entity.Info.JSON);
        AreEqual("event types: 0, handlers: 0", entity.Info.EventHandlers.ToString());
    }
    
    [Test]
    public static void Test_Entity_debugger_screenshot()
    {
        var store   = new EntityStore();
        var root    = store.CreateEntity();
        root.AddComponent(new EntityName("root"));
        
        var entity  = store.CreateEntity();
        root.AddChild(entity);

        entity.AddComponent(new EntityName("hello entity"));
        entity.AddComponent(new Position(10, 10, 0));
        entity.AddTag<MyTag>();
        var child1 = store.CreateEntity();
        var child2 = store.CreateEntity();
        var child3 = store.CreateEntity();
        child1.AddComponent(new Position(1, 1, 0));
        child2.AddComponent(new Position(1, 1, 1));
        child3.AddComponent(new Position(1, 1, 2));
        child3.AddTag<MyTag>();
            
        entity.AddChild(child1);
        entity.AddChild(child2);
        entity.AddChild(child3);
        
        DebuggerEntityScreenshot(entity);
    }
    
    private static void DebuggerEntityScreenshot(Entity entity) {
        _ = entity;
    }
}

internal struct MyTag : ITag { }
