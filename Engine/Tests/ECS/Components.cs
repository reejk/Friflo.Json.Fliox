using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Engine.ECS;

#pragma warning disable CS0649 // Field '...' is never assigned to, and will always have its default value

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable RedundantTypeDeclarationBody
namespace Tests.ECS {

// ------------------------------------------------ components
[CodeCoverageTest]
public struct MyComponent1 : IComponent {
    public          int     a;
    public override string  ToString() => a.ToString();
}

internal class CycleClass  { internal CycleClass    cycle;  }

// two classes with indirect type cycle
internal class CycleClass1 { internal CycleClass2   cycle2; }
internal class CycleClass2 { internal CycleClass1   cycle1; }

public struct MyComponent2 : IComponent { public int b; }
public struct MyComponent3 : IComponent { public int b; }
public struct MyComponent4 : IComponent { public int b; }
public struct MyComponent5 : IComponent { public int b; }
public struct MyComponent6 : IComponent { public int b; }
public struct MyComponent7 : IComponent { public int b; }

public struct NonBlittableArray         : IComponent { internal int[]                   array;  }
public struct NonBlittableList          : IComponent { internal List<int>               list;   }
public struct NonBlittableDictionary    : IComponent { internal Dictionary<int, int>    map;    }

public struct BlittableDatetime         : IComponent { public DateTime      dateTime;    }
public struct BlittableGuid             : IComponent { public Guid          guid;        }
public struct BlittableBigInteger       : IComponent { public BigInteger    bigInteger;  }

public struct NonSerializedComponent    : IComponent { public int           value;  }

[CodeCoverageTest]
public struct ByteComponent     : IComponent { public byte  b; }
public struct ShortComponent    : IComponent { public short b; }
public struct IntComponent      : IComponent { public int   b; }
public struct LongComponent     : IComponent { public long  b; }

// see [Integral numeric types - C# reference - C# | Microsoft Learn] https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
public struct NonClsTypes : IComponent
{
    public  sbyte   int8;
    public  ushort  uint16;
    public  uint    uint32;
    public  ulong   uint64;
    
    public  sbyte?  int8Null;
    public  ushort? uint16Null;
    public  uint?   uint32Null;
    public  ulong?  uint64Null;
}

public struct Component20       : IComponent
{
    public int  val1;
    public int  val2;
    public int  val3;
    public int  val4;
    public int  val5;
}

public struct Component16       : IComponent
{
    public long  val1;
    public long  val2;
}

public struct Component32       : IComponent
{
    public long  val1;
    public long  val2;
    public long  val3;
    public long  val4;
}

public struct Component64       : IComponent
{
    public long  val1;
    public long  val2;
    public long  val3;
    public long  val4;
    public long  val5;
    public long  val6;
    public long  val7;
    public long  val8;
}

/// <summary>Example shows an extension class to enable component access using less code.</summary>
public static class MyEntityExtensions
{
    public static ref MyComponent1 MyComponent1(this Entity entity) => ref entity.GetComponent<MyComponent1>();
    public static ref MyComponent2 MyComponent2(this Entity entity) => ref entity.GetComponent<MyComponent2>();
}

// test missing [StructComponent()] attribute
struct MyInvalidComponent : IComponent { public int b; }


// ------------------------------------------------ tags
public struct TestTag  : ITag { }

[CodeCoverageTest]
public struct TestTag2 : ITag { }

// Intentionally without [Tag("test-tag3")] attribute for testing
public struct TestTag3 : ITag { }

public struct TestTag4 : ITag { }

public struct TestTag5 : ITag { }

/// <summary> Used only to cover <see cref="SchemaTypeUtils.GetStructIndex"/>
/// Deprecated methods
/// TagType.NewTagIndex()
/// </summary> 
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class CodeCoverageTestAttribute : Attribute { }

}