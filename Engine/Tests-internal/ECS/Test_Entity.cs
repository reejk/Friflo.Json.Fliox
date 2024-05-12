using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable InconsistentNaming
namespace Internal.ECS {

public static class Test_Entity
{
    [Test]
    public static void Test_Entity_Components()
    {
        var store = new EntityStore(PidType.RandomPids);
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
    public static void Test_Entity_Info()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var entity  = store.CreateEntity();
        
        entity.AddComponent<Position>();
        entity.AddScript(new TestScript1());
        
        AreEqual("",                            entity.Info.ToString());
        AreEqual(entity.Pid,                    entity.Info.Pid);
        AreEqual(entity.Enabled,                entity.Info.Enabled);
        AreEqual("event types: 0, handlers: 0", entity.Info.EventHandlers.ToString());
    }
    
    [Test]
    public static void Test_Entity_Info_EventHandlers()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var entity1 = store.CreateEntity();
        entity1.OnScriptChanged         += _ => { };

        AreEqual("event types: 1, handlers: 1", entity1.Info.EventHandlers.ToString());
    }
    
    [Test]
    public static void Test_Entity_debugger_screenshot()
    {
        var store   = new EntityStore(PidType.RandomPids);
        var root    = store.CreateEntity();
        root.AddComponent(new EntityName("root"));
        
        var entity  = store.CreateEntity();
        entity.AddComponent(new EntityName("hello entity"));
        entity.AddComponent(new Position(10, 10, 0));
        entity.AddTag<MyTag>();
        
        var child1 = store.CreateEntity();
        child1.AddComponent(new Position(1, 1, 0));
        child1.Enabled = false;
        
        DebuggerEntityScreenshot(entity);
    }
    
    // make screenshot with SCale 350% Scale - on 4K monitor. Size[px]: 1890 x 1110
    private static void DebuggerEntityScreenshot(Entity entity) {
        _ = entity;
    }
    
    [Test]
    public static void Test_Entity_EntityComponents()
    {
        var store   = new EntityStore(PidType.RandomPids);
        var entity  = store.CreateEntity();
        entity.AddComponent<Position>();
        entity.AddComponent<Rotation>();
        
        var debugView   = new EntityComponentsDebugView(entity.Components);
        var components  = debugView.Components;
        
        AreEqual(2, components.Length);
        AreEqual(new Position(), components[0]);
        AreEqual(new Rotation(), components[1]);
    }
    
    [Test]
    public static void Test_Entity_Scripts_DebugView()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var entity  = store.CreateEntity();
        var script = new TestScript1();
        entity.AddScript(script);
        
        var debugView   = new ScriptsDebugView(entity.Scripts);
        var scripts     = debugView.Items;
        
        AreEqual(1,     scripts .Length);
        AreSame(script, scripts[0]);
    }
}

internal struct MyTag : ITag { }

}

