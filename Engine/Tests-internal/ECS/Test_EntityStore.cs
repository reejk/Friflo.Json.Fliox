using System;
using Friflo.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Internal.ECS {

public static class Test_EntityStore
{
    /// <summary>Cover <see cref="EntityStoreBase.AddArchetype"/></summary>
    [Test]
    public static void Test_Tags_cover_AddArchetype() {
        var store       = new EntityStore(PidType.RandomPids);
        var archetype   = store.GetArchetype(ComponentTypes.Get<Position>());
        
        archetype.SetInternalField(nameof(archetype.archIndex), 5);
        
        var e = Throws<InvalidOperationException>(() => {
            EntityStoreBase.AddArchetype(store, archetype);
        });
        AreEqual("invalid archIndex. expect: 2, was: 5", e!.Message);
    }
    
    /// <summary>Test id assignment in <see cref="EntityStore.EnsureNodesLength"/></summary>
    [Test]
    public static void Test_EntityStore_EnsureNodesLength()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        for (int n = 0; n < 10; n++) {
            var nodes = store.nodes;
            for (int i = 0; i < nodes.Length; i++) {
                AreEqual(i, nodes[i].Id);
            }
            store.CreateEntity();
        }
    }
}

}

