// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using System;
using System.Numerics;

#pragma warning disable 0169

namespace UnitTest.Flow.Graph {

public class Order {
    [Fri.Property(Required = true)]
    string           id;
    string           customer;
    DateTime         created;
    List<OrderItem>  items;
}

public class OrderItem {
    [Fri.Property(Required = true)]
    string  article;
    int     amount;
    string  name;
}

public class Customer {
    [Fri.Property(Required = true)]
    string  id;
    [Fri.Property(Required = true)]
    string  name;
}

public class Article {
    [Fri.Property(Required = true)]
    string  id;
    [Fri.Property(Required = true)]
    string  name;
    string  producer;
}

public class Producer {
    [Fri.Property(Required = true)]
    string        id;
    [Fri.Property(Required = true)]
    string        name;
    List<string>  employees;
}

public class Employee {
    [Fri.Property(Required = true)]
    string  id;
    [Fri.Property(Required = true)]
    string  firstName;
    string  lastName;
}

public class TestType {
    [Fri.Property(Required = true)]
    string        id;
    DateTime      dateTime;
    DateTime?     dateTimeNull;
    BigInteger    bigInt;
    BigInteger?   bigIntNull;
    bool          boolean;
    bool?         booleanNull;
    byte          uint8;
    byte?         uint8Null;
    short         int16;
    short?        int16Null;
    int           int32;
    int?          int32Null;
    long          int64;
    long?         int64Null;
    float         float32;
    float?        float32Null;
    double        float64;
    double?       float64Null;
    PocStruct     pocStruct;
    PocStruct?    pocStructNull;
    [Fri.Property(Required = true)]
    List<int>     intArray;
    List<int>     intArrayNull;
    JsonValue     jsonValue;
    [Fri.Property(Required = true)]
    DerivedClass  derivedClass;
    DerivedClass  derivedClassNull;
}

public struct PocStruct {
    int  val;
}

public class DerivedClass : OrderItem {
    int  derivedVal;
}

}

