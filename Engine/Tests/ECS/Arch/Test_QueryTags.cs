﻿using System.Collections.Generic;
using Friflo.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.Arch;

public static class Test_QueryTags
{
    [Test]
    public static void Test_Tags_AllTags()
    {
        var store   = CreateTestStore();
        var sig     = Signature.Get<Position>();
        var query   = store.Query(sig);
        
        query = query.AllTags(Tags.Get<TestTag>());
        AreEqual("2, 7, 8, 9, 10",  query.Ids());
        
        query = query.AllTags(Tags.Get<TestTag, TestTag2>());
        AreEqual("7, 8, 9, 10",     query.Ids());
        
        query = query.AllTags(Tags.Get<TestTag, TestTag2, TestTag3>());
        AreEqual("8, 9, 10",        query.Ids());
        
        query = query.AllTags(Tags.Get<TestTag, TestTag2, TestTag3, TestTag4>());
        AreEqual("9, 10",           query.Ids());

        query = query.AllTags(Tags.Get<TestTag, TestTag2, TestTag3, TestTag4, TestTag5>());
        AreEqual("10",              query.Ids());
    }
    
    [Test]
    public static void Test_Tags_AnyTags()
    {
        var store   = CreateTestStore();
        var sig     = Signature.Get<Position>();
        var query   = store.Query(sig);
        
        query = query.AnyTags(Tags.Get<TestTag>());
        AreEqual("1, 2, 3, 4, 5, 6, 7, 8, 9, 10", query.Ids());
        
        query = query.AnyTags(Tags.Get<TestTag2>());
        AreEqual("1, 2, 3, 4, 5, 6, 7, 8, 9, 10", query.Ids());
        
        query = query.AnyTags(Tags.Get<TestTag3>());
        AreEqual("1, 2, 3, 4, 5, 6, 7, 8, 9, 10", query.Ids());
        
        query = query.AnyTags(Tags.Get<TestTag4>());
        AreEqual("1, 2, 3, 4, 5, 6, 7, 8, 9, 10", query.Ids());

        query = query.AnyTags(Tags.Get<TestTag5>());
        AreEqual("1, 2, 3, 4, 5, 6, 7, 8, 9, 10", query.Ids());
    }
    
    [Test]
    public static void Test_Tags_WithoutAnyTags()
    {
        var store   = CreateTestStore();
        var sig     = Signature.Get<Position>();
        var query   = store.Query(sig);
        
        // --- WithoutAnyTags()
        query = query.WithoutAnyTags(Tags.Get<TestTag>());
        AreEqual("1, 3, 4, 5, 6",           query.Ids());
        
        query = query.WithoutAnyTags(Tags.Get<TestTag2>());
        AreEqual("1, 2, 4, 5, 6",           query.Ids());
        
        query = query.WithoutAnyTags(Tags.Get<TestTag3>());
        AreEqual("1, 2, 3, 5, 6, 7",        query.Ids());
        
        query = query.WithoutAnyTags(Tags.Get<TestTag4>());
        AreEqual("1, 2, 3, 4, 6, 7, 8",     query.Ids());

        query = query.WithoutAnyTags(Tags.Get<TestTag5>());
        AreEqual("1, 2, 3, 4, 5, 7, 8, 9",  query.Ids());
    }
    
    private static EntityStore CreateTestStore()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        CreateEntity(store,  1, new Tags());    
        
        CreateEntity(store,  2, Tags.Get<TestTag>());
        CreateEntity(store,  3, Tags.Get<TestTag2>());
        CreateEntity(store,  4, Tags.Get<TestTag3>());
        CreateEntity(store,  5, Tags.Get<TestTag4>());
        CreateEntity(store,  6, Tags.Get<TestTag5>());
        
        CreateEntity(store,  7, Tags.Get<TestTag, TestTag2>());
        CreateEntity(store,  8, Tags.Get<TestTag, TestTag2, TestTag3>());
        CreateEntity(store,  9, Tags.Get<TestTag, TestTag2, TestTag3, TestTag4>());
        CreateEntity(store, 10, Tags.Get<TestTag, TestTag2, TestTag3, TestTag4, TestTag5>());
        return store;
    }
    
    private static void CreateEntity(EntityStore store, int id, in Tags tags)
    {
        var entity = store.CreateEntity(id);
        entity.AddComponent<Position>();
        entity.AddTags(tags);
    }
    
    private static string Ids(this ArchetypeQuery entities)
    {
        var list = new List<int>();
        foreach (var entity in entities.Entities) {
            list.Add(entity.Id);
        }
        return string.Join(", ", list);
    }
}

