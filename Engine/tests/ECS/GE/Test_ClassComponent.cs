using System;
using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable InconsistentNaming
namespace Tests.ECS.GE;


public static class Test_ClassComponent
{
    private const long Count = 10; // 1_000_000_000L
    
    [Test]
    public static void Test_1_AddComponent() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        AreEqual("id: 1  []",   player.ToString());
        AreSame(store,          player.Archetype.Store);
        
        // --- add class component
        var testRef1 = new TestRefComponent1 { val1 = 1 };
        IsNull(player.AddClassComponent(testRef1));
        NotNull(testRef1.Entity);
        AreSame(testRef1,       player.GetClassComponent<TestRefComponent1>());
        AreEqual(1,             player.ClassComponents.Length);
        AreEqual("id: 1  [*TestRefComponent1]", player.ToString());
        AreEqual(1,             player.ClassComponents.Length);
        AreSame (testRef1,      player.ClassComponents[0]);
        
        var e = Throws<InvalidOperationException> (() => {
            player.AddClassComponent(testRef1);
        });
        AreEqual("component already added to an entity", e!.Message);
        AreEqual(1,             player.ClassComponents.Length);
        
        var testRef2 = new TestRefComponent2 { val2 = 2 };
        IsNull (player.AddClassComponent(testRef2));
        NotNull (testRef2.Entity);
        
        AreSame (testRef2,      player.GetClassComponent<TestRefComponent2>());
        AreEqual(2,             player.ClassComponents.Length);
        AreEqual("id: 1  [*TestRefComponent1, *TestRefComponent2]", player.ToString());
        
        var testRef3 = new TestRefComponent2();
        NotNull (player.AddClassComponent(testRef3));
        IsNull  (testRef2.Entity);
        NotNull (testRef3.Entity);
        AreSame (testRef3,      player.GetClassComponent<TestRefComponent2>());
        AreEqual(2,             player.ClassComponents.Length);
        AreEqual("id: 1  [*TestRefComponent1, *TestRefComponent2]", player.ToString());
        
        // IsTrue(ClassUtils.RegisteredClassComponentKeys.ContainsKey(typeof(TestRefComponent1)));
        
        for (long n = 0; n < Count; n++) {
            _ = player.GetClassComponent<TestRefComponent1>();
        }
    }
    
    [Test]
    public static void Test_2_RemoveClassComponent() {
        var store   = new GameEntityStore();
        var player = store.CreateEntity();
        
        var testRef1 = new TestRefComponent1();
        IsFalse(player.TryGetClassComponent<TestRefComponent1>(out _));
        IsNull(player.RemoveClassComponent<TestRefComponent1>());
        AreEqual("id: 1  []",                   player.ToString());
        AreEqual("[*TestRefComponent1]",        testRef1.ToString());
        
        player.AddClassComponent(testRef1);
        AreSame(testRef1, player.GetClassComponent<TestRefComponent1>());
        IsTrue(player.TryGetClassComponent<TestRefComponent1>(out var result));
        AreSame(testRef1, result);
        AreEqual("id: 1  [*TestRefComponent1]", player.ToString());
        NotNull(testRef1.Entity);
        IsFalse(player.TryGetClassComponent<TestRefComponent2>(out _)); // classComponents.Length > 0
        
        NotNull(player.RemoveClassComponent<TestRefComponent1>());
        IsNull(player.GetClassComponent<TestRefComponent1>());
        IsFalse(player.TryGetClassComponent<TestRefComponent1>(out _));
        AreEqual("id: 1  []",                   player.ToString());
        IsNull(testRef1.Entity);
        
        IsNull(player.RemoveClassComponent<TestRefComponent1>());
    }
    
    [Test]
    public static void Test_3_RemoveClassComponent() {
        var store   = new GameEntityStore();
        var player = store.CreateEntity();
        
        IsNull (player.AddClassComponent(new TestRefComponent1 { val1 = 1 }));
        IsNull (player.AddClassComponent(new TestRefComponent2 { val2 = 2 }));
        IsNull (player.AddClassComponent(new TestRefComponent3 { val3 = 3 }));
        NotNull(player.RemoveClassComponent<TestRefComponent2>());
        
        NotNull(player.GetClassComponent<TestRefComponent1>());
        IsNull (player.GetClassComponent<TestRefComponent2>());
        NotNull(player.GetClassComponent<TestRefComponent3>());
    }
    
    [Test]
    public static void Test_3_InvalidRefComponent() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        
        var testRef1 = new InvalidRefComponent();
        var e = Throws<InvalidOperationException>(() => {
            player.AddClassComponent(testRef1); 
        });
        AreEqual("Missing attribute [ClassComponent(\"<key>\")] on type: Tests.ECS.InvalidRefComponent", e!.Message);
        AreEqual(0, player.ClassComponents.Length);
        
        var component = player.GetClassComponent<InvalidRefComponent>();
        IsNull(component);
        
        // throws currently no exception
        player.RemoveClassComponent<InvalidRefComponent>();
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
    public static void Test_GetClassComponent_Perf() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        player.AddClassComponent(new TestRefComponent1());
        NotNull(player.GetClassComponent<TestRefComponent1>());
        
        const int count = 10; // 1_000_000_000 ~ 5.730 ms
        for (long n = 0; n < count; n++) {
            player.GetClassComponent<TestRefComponent1>();
        }
    }
    
    [Test]
    public static void Test_3_Perf_Add_Remove_Component() {
        var store   = new GameEntityStore();
        var player  = store.CreateEntity();
        AreEqual("id: 1  []", player.ToString());
        
        const int count = 10; // 100_000_000 ~ 4.534 ms
        for (long n = 0; n < count; n++) {
            var testRef1 = new TestRefComponent1();
            player.AddClassComponent(testRef1);
            player.RemoveClassComponent<TestRefComponent1>();
        }
    }
    
    [ClassComponent("empty")]
    private class EmptyClassComponent : ClassComponent { }
    
    [Test]
    public static void Test_Empty_Lifecycle_methods() {
        var empty = new EmptyClassComponent();
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
        entity.AddClassComponent(test);                 // struct component added via editor
        entity.AddComponent(new Position { x = 1 });    // class  component added via editor
        entity.AddComponent(new MyComponent1 { a = 1}); // class  component added via editor
        
        AreEqual(1, entity.ClassComponents.Length);
        AreEqual(2, entity.Archetype.ComponentCount);
        AreEqual("id: 1  [*TestComponent, Position, MyComponent1]", entity.ToString());
        AreSame(entity, test.Entity);
        test.Start();
        test.Update();
    }
}




