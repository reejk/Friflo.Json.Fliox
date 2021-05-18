﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Sync;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Graph
{
    public class TestDatabase : EntityDatabase
    {
        private readonly    EntityDatabase  local;
        
        public TestDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            if (TryGetContainer(name, out EntityContainer container)) {
                return container;
            }
            EntityContainer localContainer = local.GetOrCreateContainer(name);
            return new TestContainer(name, this, localContainer);
        }

        public TestContainer GetTestContainer(string name) {
            return (TestContainer) GetOrCreateContainer(name);
        }
    }
    
    public class TestContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        public  readonly    Dictionary<string, string>  readErrors  = new Dictionary<string, string>();
        public  readonly    Dictionary<string, string>  writeErrors = new Dictionary<string, string>();
        public  readonly    Dictionary<string, string>  queryErrors = new Dictionary<string, string>();
        
        public  override    bool            Pretty       => local.Pretty;
        public  override    SyncContext     SyncContext  => local.SyncContext;

        public TestContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }

        public override async Task<CreateEntitiesResult>    CreateEntities  (CreateEntities command) {
            var error = SimulateWriteErrors(command.entities.Keys.ToHashSet(), out var errors);
            if (error != null)
                return new CreateEntitiesResult {Error = error};
            if (errors != null)
                return new CreateEntitiesResult {createErrors = errors};
            return await local.CreateEntities(command);
        }

        public override async Task<UpdateEntitiesResult>    UpdateEntities  (UpdateEntities command) {
            var error = SimulateWriteErrors(command.entities.Keys.ToHashSet(), out var errors);
            if (error != null)
                return new UpdateEntitiesResult {Error = error};
            if (errors != null)
                return new UpdateEntitiesResult {updateErrors = errors};
            return await local.UpdateEntities(command);
        }
        
        public override async Task<DeleteEntitiesResult>    DeleteEntities  (DeleteEntities command) {
            var error = SimulateWriteErrors(command.ids, out var errors);
            if (error != null)
                return new DeleteEntitiesResult {Error = error};
            if (errors != null)
                return new DeleteEntitiesResult {deleteErrors = errors};
            return await local.DeleteEntities(command);
        }

        public override async Task<ReadEntitiesResult>      ReadEntities    (ReadEntities command) {
            var result = await local.ReadEntities(command);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null)
                result.Error = databaseError;
            return result;
        }
        
        public override async Task<QueryEntitiesResult>     QueryEntities   (QueryEntities command) {
            var result = await local.QueryEntities(command);
            var databaseError = SimulateReadErrors(result.entities);
            if (databaseError != null) {
                result.Error = databaseError;
                return result;
            }
            if (queryErrors.TryGetValue(command.filterLinq, out string filterLinq)) {
                switch (filterLinq) {
                    case Simulate.QueryTaskException:
                        throw new SimulationException("simulated query exception");
                    case Simulate.QueryTaskError:
                        return new QueryEntitiesResult {Error = new CommandError {message = "simulated query error"}};
                }
            }
            return result;
        }
        
        
        // --- simulate read/write error methods
        private CommandError SimulateReadErrors(Dictionary<string,EntityValue> entities) {
            foreach (var readPair in readErrors) {
                var id      = readPair.Key;
                if (entities.TryGetValue(id, out EntityValue value)) {
                    var payload = readPair.Value;
                    switch (payload) {
                        case Simulate.ReadEntityError:
                            value.SetJson("null");
                            var error = new EntityError(EntityErrorType.ReadError, name, id, "simulated read entity error");
                            value.SetError(error);
                            break;
                        case Simulate.ReadTaskError:
                            return new CommandError{message = "simulated read task error"};
                        case Simulate.ReadTaskException:
                            throw new SimulationException("simulated read task exception");
                        default:
                            value.SetJson(payload); // modify JSON
                            break;
                    }
                }
            }
            return null;
        }
        
        private CommandError SimulateWriteErrors(HashSet<string> entities, out Dictionary<string, EntityError> errors) {
            errors = null;
            foreach (var errorPair in writeErrors) {
                var id = errorPair.Key;
                if (entities.Contains(id)) {
                    var error = errorPair.Value;
                    switch (error) {
                        case Simulate.WriteTaskException:
                            throw new SimulationException("simulated write task exception");
                        case Simulate.WriteTaskError:
                            return new CommandError {message = "simulated write task error"};
                        case Simulate.WriteEntityError:
                            if (errors == null)
                                errors = new Dictionary<string, EntityError>();
                            var entityError = new EntityError(EntityErrorType.WriteError, name, id, "simulated write entity error");
                            errors.Add(id, entityError);
                            break;
                    }
                }
            }
            return null;
        }
    }

    public static class Simulate
    {
        public const string ReadEntityError     = "READ-ENTITY-ERROR";
        public const string WriteEntityError    = "WRITE-ENTITY-ERROR";

        public const string ReadTaskError       = "READ-TASK-ERROR";
        public const string ReadTaskException   = "READ-TASK-EXCEPTION";
        
        public const string QueryTaskException  = "QUERY-TASK-EXCEPTION";
        public const string QueryTaskError      = "QUERY-TASK-ERROR";
        
        public const string WriteTaskException  = "WRITE-TASK-EXCEPTION";
        public const string WriteTaskError      = "WRITE-TASK-ERROR";
    }    

    public class SimulationException : Exception {
        public SimulationException(string message) : base(message) { }
    }
}
