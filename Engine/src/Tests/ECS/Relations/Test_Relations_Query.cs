﻿using System;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable AccessToModifiedClosure
// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Relations {

public static class Test_Relations_Query
{
    [Test]
    public static void Test_Relations_query()
    {
        var store    = new EntityStore();
        var entity0  = store.CreateEntity(100);
        var emptyRelations = entity0.GetRelations<AttackRelation>();
        AreEqual(0, emptyRelations.Length);
        
        var entity1  = store.CreateEntity(1);
        var entity2  = store.CreateEntity(2);
        var entity3  = store.CreateEntity(3);
        
        var target10 = store.CreateEntity();
        var target11 = store.CreateEntity();
        var target12 = store.CreateEntity();
        
        entity1.AddComponent(new AttackRelation { target = target10, speed = 42 });
        
        entity2.AddComponent(new AttackRelation { target = target10, speed = 20 });
        entity2.AddComponent(new AttackRelation { target = target11, speed = 21 });
        
        entity3.AddComponent(new Position());
        entity3.AddComponent(new AttackRelation { target = target10, speed = 10 });
        entity3.AddComponent(new AttackRelation { target = target11, speed = 11 });
        entity3.AddComponent(new AttackRelation { target = target12, speed = 12 });
        
        emptyRelations = entity0.GetRelations<AttackRelation>();
        AreEqual(0, emptyRelations.Length);
        
        // --- query
        var query = store.Query<AttackRelation>();
        int count = 0;
        query.ForEachEntity((ref AttackRelation relation, Entity entity) => {
            switch (count++) {
                case 0: Mem.AreEqual(42, relation.speed); break;
                case 1: Mem.AreEqual(20, relation.speed); break;
                case 2: Mem.AreEqual(21, relation.speed); break;
                case 3: Mem.AreEqual(10, relation.speed); break;
                case 4: Mem.AreEqual(11, relation.speed); break;
                case 5: Mem.AreEqual(12, relation.speed); break;
            }
        });
        Mem.AreEqual(6, count);
        Mem.AreEqual(6, query.Count);
        
        var start = Mem.GetAllocatedBytes();
        count = 0;
        foreach (var entity in query.Entities) {
            count++;
            var relationCount = 0;
            var relations = entity.GetRelations<AttackRelation>();
            switch (entity.Id) {
                case 1:
                    Mem.AreEqual(1,  relations.Length);
                    Mem.AreEqual(42, relations[0].speed);
                    foreach (var relation in relations) {
                        switch (relationCount++) {
                            case 0: Mem.AreEqual(42, relation.speed); break;
                        }
                    }
                    Mem.AreEqual(1, relationCount);
                    break;
                case 2:
                    Mem.AreEqual(2,  relations.Length);
                    Mem.AreEqual(20, relations[0].speed);
                    Mem.AreEqual(21, relations[1].speed);
                    foreach (var relation in relations) {
                        switch (relationCount++) {
                            case 0: Mem.AreEqual(20, relation.speed); break;
                            case 1: Mem.AreEqual(21, relation.speed); break;
                        }
                    }
                    Mem.AreEqual(2, relationCount);
                    break;
                case 3:
                    Mem.AreEqual(3,  relations.Length);
                    Mem.AreEqual(10, relations[0].speed);
                    Mem.AreEqual(11, relations[1].speed);
                    Mem.AreEqual(12, relations[2].speed);
                    foreach (var relation in relations) {
                        switch (relationCount++) {
                            case 0: Mem.AreEqual(10, relation.speed); break;
                            case 1: Mem.AreEqual(11, relation.speed); break;
                            case 2: Mem.AreEqual(12, relation.speed); break;
                        }
                    }
                    Mem.AreEqual(3, relationCount);
                    break;
            }
        }
        Mem.AreEqual(6, count);
        Mem.AssertNoAlloc(start);
        
        // --- test with additional filter condition 
        query.WithoutAnyComponents(ComponentTypes.Get<Position>());
        AreEqual(3, query.Count);
        count = 0;
        foreach (var entity in query.Entities) {
            var relations = entity.GetRelations<AttackRelation>();
            switch (count++) {
                case 0: Mem.AreEqual(1, relations.Length);  break;
                case 1: Mem.AreEqual(2, relations.Length);  break;
                case 2: Mem.AreEqual(2, relations.Length);  break;
            }
        }
        Mem.AreEqual(3, count);
    }
    
    [Test]
    public static void Test_Relations_query_exception()
    {
        var store    = new EntityStore();
        var e = Throws<InvalidOperationException>(() => {
            store.Query<AttackRelation, Position>();
        });
        AreEqual("relation component query cannot have other query components", e!.Message);
    }
}

}
