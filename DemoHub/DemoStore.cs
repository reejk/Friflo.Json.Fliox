﻿using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Req = Friflo.Json.Fliox.Mapper.Fri.RequiredAttribute;

// ReSharper disable All
namespace Fliox.DemoHub
{
    /// <summary>
    /// The <see cref="DemoStore"/> offer two functionalities: <br/>
    /// 1. Defines a database <b>schema</b> by declaring its containers, commands and messages<br/>
    /// 2. Is a database <b>client</b> providing type-safe access to its containers, commands and messages <br/>
    /// <br/>
    /// <i>Info</i>: Use command <b>demo.FakeRecords</b> to create fake records in various containers. <br/>
    /// </summary>
    /// <remarks>
    /// <see cref="DemoStore"/> containers are fields or properties of type <see cref="EntitySet{TKey,T}"/>. <br/>
    /// Its commands are methods returning a <see cref="CommandTask{TResult}"/>. See ./DemoStore-commands.cs <br/>
    /// Its messages are methods returning a <see cref="MessageTask"/>. <br/>
    /// <br/>
    /// <see cref="DemoStore"/> instances can be used on client and server side. <br/>
    /// The <see cref="MessageHandler"/> demonstates how to use a <see cref="DemoStore"/> instances as client to execute
    /// common database operations like: Upsert, Count and Query. <br/>
    /// </remarks>
    [Fri.OpenAPI(Version = "1.0.0",
        ContactName = "Ullrich Praetz", ContactUrl = "https://github.com/friflo/Friflo.Json.Fliox/issues",
        LicenseName = "MIT",            LicenseUrl = "https://spdx.org/licenses/MIT.html")]
    [Fri.OpenAPIServer(Description = "public DemoHub API", Url = "http://ec2-174-129-178-18.compute-1.amazonaws.com/fliox/rest/main_db")]
    public partial class DemoStore : FlioxClient {
        // --- containers
        public readonly EntitySet <long, Article>     articles;
        public readonly EntitySet <long, Customer>    customers;
        public readonly EntitySet <long, Employee>    employees;
        public readonly EntitySet <long, Order>       orders;
        public readonly EntitySet <long, Producer>    producers;

        public DemoStore(FlioxHub hub) : base (hub) { }
    }
    
    // ------------------------------ entity models ------------------------------
    public class Article {
        [Req]   public  long                id { get; set; }
        [Req]   public  string              name;
                public  Ref<long, Producer> producer;
                public  DateTime?           created;
    }

    public class Customer {
        [Req]   public  long                id { get; set; }
        [Req]   public  string              name;
                public  DateTime?           created;
    }
    
    public class Employee {
        [Req]   public  long                id { get; set; }
        [Req]   public  string              firstName;
                public  string              lastName;
                public  DateTime?           created;
    }

    public class Order {
        [Req]   public  long                id { get; set; }
                public  Ref<long, Customer> customer;
                public  DateTime            created;
                public  List<OrderItem>     items = new List<OrderItem>();
    }

    public class OrderItem {
        [Req]   public  Ref<long, Article>  article;
                public  int                 amount;
                public  string              name;
    }

    public class Producer {
        [Req]   public  long                        id { get; set; }
        [Req]   public  string                      name;
                public  List<Ref<long, Employee>>   employees;
                public  DateTime?                   created;
    }
}
