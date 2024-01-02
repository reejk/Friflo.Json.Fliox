﻿using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Utils;
using NUnit.Framework;
using Tests.ECS;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Internal.ECS;

public static class Test_sizeof
{
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('EntityNode')
    // ------------------------------------------ Engine types ------------------------------------------
    [Test]
    public static unsafe void Test_sizeof_EntityNode() {
        var size = sizeof(EntityNode);
        AreEqual(48, size);
    }
    
    [Test]
    public static unsafe void Test_sizeof_Entity() {
        var size = sizeof(Entity);
        AreEqual(16, size);
    }
    
    [Test]
    public static unsafe void Test_sizeof_RawEntity() {
        var size = sizeof(RawEntity);
        AreEqual(8, size);
        
        var rawEntity = new RawEntity { archIndex = 1, compIndex = 2 };
        AreEqual("archIndex: 1  compIndex: 2", rawEntity.ToString());
    }
    
    [Test]
    public static unsafe void Test_sizeof_BitSet() {
        var size = sizeof(BitSet);
        AreEqual(32, size);
    }
    
    [Test]
    public static unsafe void Test_sizeof_Tags() {
        var size = sizeof(Tags);
        AreEqual(32, size);
    }
    
    [Test]
    public static unsafe void Test_sizeof_ComponentTypes() {
        var size = sizeof(ComponentTypes);
        AreEqual(32, size);
    }
        
    [Test]
    public static void Test_sizeof_StructIndexes() {
        var type = typeof(SignatureIndexes);
        var size = Marshal.SizeOf(type!);
        AreEqual(24, size);
    }
    
    [Test]
    public static unsafe void Test_Math_sizeof() {
        var size = sizeof(Position);
        AreEqual(12, size);
        
        size = sizeof(Rotation);
        AreEqual(16, size);
        
        size = sizeof(Scale3);
        AreEqual(12, size);
        
        size = sizeof(Transform);
        AreEqual(64, size);
    }
    
    [Test]
    public static unsafe void Test_Query_sizeof() {
        var size = sizeof(ChunkEntities);
        AreEqual(24, size);
        
        size = sizeof(Chunk<Position>);
        AreEqual(16, size);

        size = sizeof(Archetypes);
        AreEqual(16, size);
        
        size = sizeof(QueryChunks<Position>);
        AreEqual(8, size);
        
        size = sizeof(ChunkEnumerator<Position>);
        AreEqual(96, size);
    }
    
#if COMP_ITER
    [Test]
    public static void Test_Ref_ToString()
    {
        var refPosition = new Ref<Position>();
        var positions   = new [] { new Position(1, 2, 3) };
        refPosition.Set(positions, null, 1);
        refPosition.pos = 0;
        AreEqual("1, 2, 3", refPosition.ToString());
    }
#endif
    
    [Test]
    public static unsafe void Test_Scripts_sizeof()
    {
        var size = sizeof(EntityScripts);
        AreEqual(16, size);
        
        var scripts       = new EntityScripts();
        AreEqual("unused", scripts.ToString());
        
        var scriptsArray  = new Script[] { new TestScript() };
        scripts           = new EntityScripts(1, scriptsArray);
        AreEqual("id: 1  [*TestScript]", scripts.ToString());
    }
    
    class TestScript : Script { }
    
    // ---------------------------------------- Tests project types ------------------------------------------
    [Test]
    public static unsafe void Test_sizeof_ByteComponent() {

        var type = typeof(ByteComponent);
        var size = Marshal.SizeOf(type!);
        AreEqual(1, size);
        
        var bytes = new ByteComponent[10];
        fixed (ByteComponent* item0 = &bytes[0])
        fixed (ByteComponent* item1 = &bytes[1])
        {
            var offset = item1 - item0;
            AreEqual(1L, offset);
        }
    }
}