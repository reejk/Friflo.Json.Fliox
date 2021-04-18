﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Graph.Query;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.EntityGraph.Database
{
    // ------------------------------ SyncRequest / SyncResponse ------------------------------
    public class SyncRequest
    {
        public  List<DatabaseCommand>                   commands;
    }
    
    public partial class SyncResponse
    {
        public  List<CommandResult>                     results;
        public  Dictionary<string, ContainerEntities>   containerResults;
    }
    
    // ------ ContainerEntities
    public partial class ContainerEntities
    {
        public  string                          container; // only for debugging
        public  Dictionary<string, EntityValue> entities;
    }
    
    // ------------------------------ DatabaseCommand ------------------------------
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntities),          Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntities),            Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntities),           Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntities),           Discriminant = "patch")]
    public abstract class DatabaseCommand
    {
        internal abstract CommandResult   Execute(EntityDatabase database, SyncResponse response);
        internal abstract CommandType     CommandType { get; }
    }
    
    // ------------------------------ CommandResult ------------------------------
    [Fri.Discriminator("command")]
    [Fri.Polymorph(typeof(CreateEntitiesResult),    Discriminant = "create")]
    [Fri.Polymorph(typeof(ReadEntitiesResult),      Discriminant = "read")]
    [Fri.Polymorph(typeof(QueryEntitiesResult),     Discriminant = "query")]
    [Fri.Polymorph(typeof(PatchEntitiesResult),     Discriminant = "patch")]
    public abstract class CommandResult
    {
        internal abstract CommandType CommandType { get; }
    }
    
    public enum CommandType
    {
        Read,
        Query,
        Create,
        Patch
    }
    
    // ------ CreateEntities
    public partial class CreateEntities : DatabaseCommand
    {
        public  string                          container;
        public  Dictionary<string, EntityValue> entities;
    }
    
    public class CreateEntitiesResult : CommandResult
    {
        internal override CommandType CommandType => CommandType.Create;
    }

    // ------ ReadEntities
    public partial class ReadEntities : DatabaseCommand
    {
        public  string                      container;
        public  List<string>                ids;
        public  List<ReadReference>         references;
    }
    
    /// The data of requested entities are added to <see cref="ContainerEntities.entities"/> 
    public partial class ReadEntitiesResult : CommandResult
    {
        public  List<ReadReferenceResult>   references;
    }
    
    // ------ ReadReference
    public class ReadReference
    {
        /// Path to a <see cref="Ref{T}"/> field referencing an <see cref="Entity"/>.
        /// These referenced entities are also loaded via the next <see cref="EntityStore.Sync"/> request.
        public  string              refPath; // e.g. ".items[*].article"
        public  string              container;
        public  List<string>        ids;
    }
    
    public class ReadReferenceResult
    {
        public  string              container;
        public  List<string>        ids;
    }
    
    // ------ QueryEntities
    public class QueryEntities : DatabaseCommand
    {
        public  string                      container;
        public  BoolOp                      filter;
        public  List<ReadReference>         references;

        internal override CommandType       CommandType => CommandType.Query;
        
        internal override CommandResult Execute(EntityDatabase database, SyncResponse response) {
            throw new System.NotImplementedException();
        }
    }
    
    public class QueryEntitiesResult : CommandResult
    {
        public              List<ReadReferenceResult>   references;
        
        internal override   CommandType                 CommandType => CommandType.Query;
    }
    
    // ------ PatchEntities
    public partial class PatchEntities : DatabaseCommand
    {
        public  string              container;
        public  List<EntityPatch>   entityPatches;
    }

    public class EntityPatch
    {
        public string               id;
        public List<JsonPatch>      patches;
    }

    public class PatchEntitiesResult : CommandResult
    {
        internal override CommandType CommandType => CommandType.Patch;
    }
}
