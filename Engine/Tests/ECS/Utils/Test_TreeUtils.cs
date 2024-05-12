// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Serialize;
using Friflo.Engine.ECS.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable ConvertToConstant.Local
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Utils {


public static class Test_TreeUtils
{
#region Duplicate Entity's
    /// <summary> Cover <see cref="TreeUtils.DuplicateEntities"/> and <see cref="TreeUtils.DuplicateChildren"/> </summary>
    [Test]
    public static void Test_TreeUtils_DuplicateEntities()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var root    = store.CreateEntity(1);
        var child2  = store.CreateEntity(2);
        var child3  = store.CreateEntity(3);
        child2.AddComponent(new EntityName("child-2"));
        child3.AddComponent(new EntityName("child-3"));
        
        store.SetStoreRoot(root);
        root.AddChild(child2);
        child2.AddChild(child3);
        AreEqual(1,             root.ChildCount);
        AreEqual(1,             child2.ChildCount);
        
        // --- Duplicate two child entities
        var entities    = new List<Entity>{ child2, child3 };
        var indexes     = TreeUtils.DuplicateEntities(entities);
        
        AreEqual(2,             indexes.Length);
        AreEqual(2,             root.ChildCount);
        AreEqual(2,             child2.ChildCount);
        
        var clone2 =            root.ChildEntities  [indexes[0]];
        AreEqual("child-2",     clone2.GetComponent<EntityName>().value);
        
        var clone3 =            child2.ChildEntities[indexes[1]];
        AreEqual("child-3",     clone3.GetComponent<EntityName>().value);
        
        // --- Duplicate root
        entities        = new List<Entity>{ root };
        indexes         = TreeUtils.DuplicateEntities(entities);
        
        AreEqual(2,             root.ChildCount);
        AreEqual(1,             indexes.Length);
        AreEqual(-1,            indexes[0]);
    }
    #endregion
}

}