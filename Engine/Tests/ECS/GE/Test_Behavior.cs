using System;
using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable InconsistentNaming
namespace Tests.ECS.GE;


public static class Test_Script
{
    private const long Count = 10; // 1_000_000_000L
    
    [Test]
    public static void Test_1_AddComponent() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        AreEqual("id: 1  []",   player.ToString());
        AreSame(store,          player.Archetype.Store);
        
        // --- add behavior
        var testRef1 = new TestScript1 { val1 = 1 };
        IsNull(player.AddScript(testRef1));
        NotNull(testRef1.Entity);
        AreSame(testRef1,       player.GetScript<TestScript1>());
        AreEqual(1,             player.Scripts.Length);
        AreEqual("id: 1  [*TestScript1]", player.ToString());
        AreEqual(1,             player.Scripts.Length);
        AreSame (testRef1,      player.Scripts[0]);
        AreEqual(1,             store.EntityScripts.Length);
        
        var e = Throws<InvalidOperationException> (() => {
            player.AddScript(testRef1);
        });
        AreEqual("behavior already added to an entity. current entity id: 1", e!.Message);
        AreEqual(1,             player.Scripts.Length);
        
        var testRef2 = new TestScript2 { val2 = 2 };
        IsNull (player.AddScript(testRef2));
        NotNull (testRef2.Entity);
        
        AreSame (testRef2,      player.GetScript<TestScript2>());
        AreEqual(2,             player.Scripts.Length);
        AreEqual("id: 1  [*TestScript1, *TestScript2]", player.ToString());
        AreEqual(1,             store.EntityScripts.Length);
        
        var testRef3 = new TestScript2();
        NotNull (player.AddScript(testRef3));
        IsNull  (testRef2.Entity);
        NotNull (testRef3.Entity);
        AreSame (testRef3,      player.GetScript<TestScript2>());
        AreEqual(2,             player.Scripts.Length);
        AreEqual("id: 1  [*TestScript1, *TestScript2]", player.ToString());
        
        for (long n = 0; n < Count; n++) {
            _ = player.GetScript<TestScript1>();
        }
    }
    
    [Test]
    public static void Test_2_RemoveScript() {
        var store   = new GameEntityStore();
        var player = store.CreateEntity();
        
        var testRef1 = new TestScript1();
        IsFalse(player.TryGetScript<TestScript1>(out _));
        IsNull(player.RemoveScript<TestScript1>());
        AreEqual("id: 1  []",               player.ToString());
        AreEqual(0,                         player.Scripts.Length);
        AreEqual("[*TestScript1]",          testRef1.ToString());
        
        player.AddScript(testRef1);
        AreEqual(1,                         player.Scripts.Length);
        AreSame (testRef1, player.GetScript<TestScript1>());
        IsTrue  (player.TryGetScript<TestScript1>(out var result));
        AreSame (testRef1, result);
        AreEqual("id: 1  [*TestScript1]", player.ToString());
        NotNull (testRef1.Entity);
        IsFalse (player.TryGetScript<TestScript2>(out _));
        
        NotNull (player.RemoveScript<TestScript1>());
        AreEqual(0,                         player.Scripts.Length);
        IsNull  (player.GetScript<TestScript1>());
        IsFalse (player.TryGetScript<TestScript1>(out _));
        AreEqual("id: 1  []",               player.ToString());
        IsNull(testRef1.Entity);
        
        IsNull(player.RemoveScript<TestScript1>());
        AreEqual(0,                         player.Scripts.Length);
    }
    
    [Test]
    public static void Test_3_RemoveScript() {
        var store   = new GameEntityStore();
        var player = store.CreateEntity();
        
        IsNull  (player.AddScript(new TestScript1 { val1 = 1 }));
        IsNull  (player.AddScript(new TestScript2 { val2 = 2 }));
        IsNull  (player.AddScript(new TestScript3 { val3 = 3 }));
        NotNull (player.RemoveScript<TestScript2>());
        AreEqual(2, player.Scripts.Length);
        
        NotNull(player.GetScript<TestScript1>());
        IsNull (player.GetScript<TestScript2>());
        NotNull(player.GetScript<TestScript3>());
    }
    
    /// <summary>Cover move last behavior in <see cref="GameEntityStore.RemoveScript"/> </summary>
    [Test]
    public static void Test_3_cover_move_last_behavior() {
        var store   = new GameEntityStore();
        var entity1 = store.CreateEntity();
        var entity2 = store.CreateEntity();
        
        IsNull  (entity1.AddScript(new TestScript1 { val1 = 1 }));
        IsNull  (entity2.AddScript(new TestScript2 { val2 = 2 }));
        AreEqual(1,                         entity1.Scripts.Length);
        AreEqual(1,                         entity2.Scripts.Length);
        AreEqual(2,                         store.EntityScripts.Length);
        
        NotNull (entity1.RemoveScript<TestScript1>());
        AreEqual(0,                         entity1.Scripts.Length);
        AreEqual(1,                         store.EntityScripts.Length);
        NotNull (entity2.RemoveScript<TestScript2>());
        AreEqual(0,                         entity2.Scripts.Length);
        AreEqual(0,                         store.EntityScripts.Length);
        
        IsNull  (entity1.GetScript<TestScript1>());
        IsNull  (entity2.GetScript<TestScript2>());
    }
    
    /// <summary>Cover <see cref="GameEntityUtils.RemoveScript"/></summary>
    [Test]
    public static void Test_3_cover_remove_non_added_behavior() {
        var store   = new GameEntityStore();
        var entity  = store.CreateEntity();
        
        IsNull  (entity.AddScript(new TestScript1 { val1 = 1 }));
        AreEqual(1, entity.Scripts.Length);
        
        IsNull  (entity.RemoveScript<TestScript2>());
        AreEqual(1, entity.Scripts.Length); // remains unchanged
    }
    
    [Test]
    public static void Test_3_InvalidRefComponent() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        
        var testRef1 = new InvalidRefComponent();
        var e = Throws<InvalidOperationException>(() => {
            player.AddScript(testRef1); 
        });
        AreEqual("Missing attribute [Script(\"<key>\")] on type: Tests.ECS.InvalidRefComponent", e!.Message);
        AreEqual(0, player.Scripts.Length);
        
        var behavior = player.GetScript<InvalidRefComponent>();
        IsNull  (behavior);
        
        // throws currently no exception
        player.RemoveScript<InvalidRefComponent>();
        AreEqual(0, player.Scripts.Length);
    }
    
    [Test]
    public static void Test_2_Perf() {
        var store   = new GameEntityStore();
        var list = new List<GameEntity>();
        for (long n = 0; n < 10; n++) {
            list.Add(store.CreateEntity());
        }
        IsTrue(list.Count > 0);
    }
    
    [Test]
    public static void Test_GetScript_Perf() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        player.AddScript(new TestScript1());
        NotNull(player.GetScript<TestScript1>());
        
        const int count = 10; // 1_000_000_000 ~ 5.398 ms
        for (long n = 0; n < count; n++) {
            player.GetScript<TestScript1>();
        }
    }
    
    [Test]
    public static void Test_3_Perf_Add_Remove_Component() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        AreEqual("id: 1  []", player.ToString());
        
        const int count = 10; // 100_000_000 ~ 3.038 ms
        for (long n = 0; n < count; n++) {
            var testRef1 = new TestScript1();
            player.AddScript(testRef1);
            player.RemoveScript<TestScript1>();
        }
    }
    
    [Script("empty")]
    private class EmptyScript : Script { }
    
    [Test]
    public static void Test_Empty_Lifecycle_methods() {
        var empty = new EmptyScript();
        empty.Start();
        empty.Update();
    }
    
    /* Editor Inspector would look like
    
    Entity              id 0    
    > TestComponent     health 4
    > Position          x 1     y 0     z 0
    > MyComponent1      a 1
         
    */
    [Test]
    public static void Test_3_Simulate_Editor() {
        var store   = new GameEntityStore();
        var entity  = store.CreateEntity();
        
        var test    = new TestComponent();
        entity.AddScript(test);                         // component added via editor
        entity.AddComponent(new Position { x = 1 });    // behavior added via editor
        entity.AddComponent(new MyComponent1 { a = 1}); // behavior added via editor
        
        AreEqual(1, entity.Scripts.Length);
        AreEqual(2, entity.Archetype.ComponentCount);
        AreEqual("id: 1  [*TestComponent, Position, MyComponent1]", entity.ToString());
        AreSame(entity, test.Entity);
        test.Start();
        test.Update();
    }
}




