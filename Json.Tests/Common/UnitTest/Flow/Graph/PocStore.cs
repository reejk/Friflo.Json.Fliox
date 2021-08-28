﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class PocStore : EntityStore
    {
        public readonly EntitySet <string, Order>       orders;
        public readonly EntitySet <string, Customer>    customers;
        public readonly EntitySet <string, Article>     articles;
        public readonly EntitySet <string, Producer>    producers;
        public readonly EntitySet <string, Employee>    employees;
        public readonly EntitySet <string, TestType>    types;
        
        public PocStore(EntityDatabase database, TypeStore typeStore, string clientId) : base (database, typeStore, clientId) {
            orders      = new EntitySet <string, Order>       (this);
            customers   = new EntitySet <string, Customer>    (this);
            articles    = new EntitySet <string, Article>     (this);
            producers   = new EntitySet <string, Producer>    (this);
            employees   = new EntitySet <string, Employee>    (this);
            types       = new EntitySet <string, TestType>    (this);
        }
        
        public PocStore(EntityDatabase database, string clientId) : this (database, TestGlobals.typeStore, clientId) { }
    }
    
    // ------------------------------ models ------------------------------
    public class Order : Entity {
        public  Ref<string, Customer>       customer;
        public  DateTime            created;
        public  List<OrderItem>     items = new List<OrderItem>();
    }

    public class OrderItem {
        [Fri.Required]  public  Ref<string, Article>    article;
                        public  int             amount;
                        public  string          name;
    }

    public class Article : Entity
    {
        [Fri.Required]  public  string          name;
                        public  Ref<string, Producer>   producer;
    }

    public class Customer : Entity {
        [Fri.Required]  public  string          name;
    }
    
    public class Producer : Entity {
        [Fri.Required]  public  string              name;
        [Fri.Property (Name =                       "employees")]
                        public  List<Ref<string, Employee>> employeeList;
    }
    
    public class Employee : Entity {
        [Fri.Required]  public  string  firstName;
                        public  string  lastName;
    }
    
    public class TestType : Entity {
                        public  DateTime        dateTime;
                        public  DateTime?       dateTimeNull;
                        public  BigInteger      bigInt;
                        public  BigInteger?     bigIntNull;
                
                        public  bool            boolean;
                        public  bool?           booleanNull;
                
                        public  byte            uint8;
                        public  byte?           uint8Null;
                        
                        public  short           int16;
                        public  short?          int16Null;
                        
                        public  int             int32;
                        public  int?            int32Null;
                        
                        public  long            int64;
                        public  long?           int64Null;
                        
                        public  float           float32;
                        public  float?          float32Null;
                        
                        public  double          float64;
                        public  double?         float64Null;
                
                        public  PocStruct       pocStruct;
                        public  PocStruct?      pocStructNull;

        [Fri.Required]  public  List<int>       intArray = new List<int>();
                        public  List<int>       intArrayNull;
        
                        public  JsonValue       jsonValue;
        
        [Fri.Required]  public  DerivedClass    derivedClass;
                        public  DerivedClass    derivedClassNull;
    }
    
    public struct PocStruct {
        public  int                 value;
    }
    
    public class DerivedClass : OrderItem {
        public int derivedVal;
    }

    // ------------------------------ messages ------------------------------
    class TestMessage {
        public          string  text;

        public override string  ToString() => text;
    }
}
