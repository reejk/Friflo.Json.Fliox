using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

// ReSharper disable All
namespace Friflo.Json.Tests.DB.Client
{
    // Note: Main property of all model classes
    // They are all POCO's aka Plain Old Class Objects. See https://en.wikipedia.org/wiki/Plain_old_CLR_object
    // As a result integration of these classes in other modules or libraries is comparatively easy.

    // ---------------------------------- entity models ----------------------------------
    public class TestOps {
        [Key]       public  string          id { get; set; }
    }
    
    public enum TestEnum {
        e1 = 101,
        e2 = 102
    }
    
    public class TestEnumEntity {
        [Key]       public  string          id { get; set; }
                    public  TestEnum        enumVal;
                    public  TestEnum?       enumValNull;
    }
}