// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Fliox.Hub.Host.Cluster
{
    public partial class ClusterStore : FlioxClient
    {
        public  readonly    EntitySet <string, Catalog>       catalogs;
        public  readonly    EntitySet <string, CatalogSchema> schemas;
        
        public ClusterStore (FlioxHub hub, string database = null) : base(hub, database) { }
    }
    
    public class Catalog {
        [Fri.Required]  public  string                      id;
        [Fri.Required]  public  string                      databaseType;
        [Fri.Required]  public  string[]                    containers;
                        
        public override         string                      ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
    
    public class CatalogSchema {
        [Fri.Required]  public  string                      id;
        [Fri.Required]  public  string                      schemaName;
        [Fri.Required]  public  string                      schemaPath;
        [Fri.Required]  public  Dictionary<string,string>   jsonSchemas;
                        
        public override         string                      ToString() => JsonSerializer.Serialize(this).Replace("\"", "'");
    }
}