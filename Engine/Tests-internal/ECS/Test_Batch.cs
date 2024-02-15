using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.ECS;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Internal.ECS;

public static class Test_Batch
{
    [Test]
    public static void Test_Batch_Entity()
    {
        var store = new EntityStore();
        store.OnComponentAdded      += _ => { };
        store.OnComponentRemoved    += _ => { };
        store.OnTagsChanged         += _ => { }; 
        var entity = store.CreateEntity();
        
        var batch = entity.Batch;
        Assert.AreEqual("empty", batch.ToString());
        
        batch.AddComponent  (new Position(1, 1, 1))
            .AddComponent   (new EntityName("test"))
            .RemoveComponent<Rotation>()
            .AddTag         <TestTag>()
            .RemoveTag      <TestTag2>();
        Assert.AreEqual("add: [EntityName, Position, #TestTag]  remove: [Rotation, #TestTag2]", batch.ToString());
        batch.Apply();
        
        Assert.AreEqual("id: 1  \"test\"  [EntityName, Position, #TestTag]", entity.ToString());
        Assert.AreEqual(new Position(1, 1, 1), entity.Position);
        
        var addTags     = Tags.Get<TestTag2>();
        var removeTags  = Tags.Get<TestTag>();
        
        batch = entity.Batch;
        batch.AddComponent  (new Position(2, 2, 2))
            .RemoveComponent<EntityName>()
            .AddTags        (addTags)
            .RemoveTags     (removeTags);
        Assert.AreEqual("add: [Position, #TestTag2]  remove: [EntityName, #TestTag]", batch.ToString());
        batch.Apply();
        
        Assert.AreEqual("id: 1  [Position, #TestTag2]", entity.ToString());
        Assert.AreEqual(new Position(2, 2, 2), entity.Position);
    }
    
    [Test]
    public static void Test_Batch_EntityStore_Batch()
    {
        var store = new EntityStore();
        var batch = store.BulkBatch;
        batch.AddComponent(new Position());
        batch.AddTag<TestTag>();
        
        var entity = store.CreateEntity();
        batch.ApplyTo(entity);
    }
    
    [Test]
    public static void Test_Batch_Entity_Perf()
    {
        long count      = 10; // 10_000_000 ~ #PC: 1691 ms
        var store       = new EntityStore();
        var entity      = store.CreateEntity();
        var addTags     = Tags.Get<TestTag2>();
        var removeTags  = Tags.Get<TestTag>();
        
        var sw = new Stopwatch();
        sw.Start();

        for (int n = 0; n < count; n++)
        {
            entity.Batch
                .AddComponent   (new Position(1, 1, 1))
                .AddComponent   (new EntityName("test"))
                .RemoveComponent<Rotation>()
                .AddTag         <TestTag>()
                .RemoveTag      <TestTag2>()
                .Apply();
        
            entity.Batch
                .AddComponent   (new Position(2, 2, 2))
                .RemoveComponent<EntityName>()
                .AddTags        (addTags)
                .RemoveTags     (removeTags)
                .Apply();
        }
        
        Console.WriteLine($"Entity.Batch - duration: {sw.ElapsedMilliseconds} ms");
    }
}

