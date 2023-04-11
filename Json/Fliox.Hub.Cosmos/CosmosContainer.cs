﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Utils;
using Microsoft.Azure.Cosmos;

namespace Friflo.Json.Fliox.Hub.Cosmos
{
    public sealed class CosmosContainer : EntityContainer
    {
        private  readonly   ContainerOptions    options;
        private             Container           cosmosContainer;
        public   override   bool                Pretty      { get; }
        
        internal CosmosContainer(string name, EntityDatabase database, ContainerOptions options, bool pretty)
            : base(name, database)
        {
            this.options    = options;
            Pretty          = pretty;
        }

        private async Task EnsureContainerExists() {
            if (cosmosContainer != null)
                return;
            var db          = options.database;
            var throughput  = options.throughput;
            cosmosContainer = await db.CreateContainerIfNotExistsAsync(instanceName, "/id", throughput).ConfigureAwait(false);
        }
        
        public override async Task<CreateEntitiesResult> CreateEntitiesAsync(CreateEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            using(var memory   = new ReusedMemoryStream()) {
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    var key     = entity.key;
                    CosmosUtils.WriteJson(memory, entity.value);
                    var partitionKey = new PartitionKey(key.AsString());
                    // consider using [Introducing Bulk support in the .NET SDK | Azure Cosmos DB Blog] https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
                    // todo handle error;
                    using (var _ = await cosmosContainer.CreateItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            return new CreateEntitiesResult();
        }

        public override async Task<UpsertEntitiesResult> UpsertEntitiesAsync(UpsertEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var entities = command.entities;
            using (var memory           = new ReusedMemoryStream())
            using (var pooled  = syncContext.EntityProcessor.Get()) {
                var processor = pooled.instance;
                for (int n = 0; n < entities.Count; n++) {
                    var entity  = entities[n];
                    var key     = entity.key;
                    var json = processor.ReplaceKey(entity.value, command.keyName, false, "id", out _, out string error);
                    CosmosUtils.WriteJson(memory, json);
                    var partitionKey = new PartitionKey(key.AsString());
                    // consider using [Introducing Bulk support in the .NET SDK | Azure Cosmos DB Blog] https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk/
                    // todo handle error;
                    using (var _ = await cosmosContainer.UpsertItemStreamAsync(memory, partitionKey).ConfigureAwait(false)) {
                    }
                }
            }
            return UpsertEntitiesResult.Create(syncContext, null);
        }

        public override async Task<ReadEntitiesResult> ReadEntitiesAsync(ReadEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var keys = command.ids;
            if (keys.Count > 1) {
                return await ReadManyEntities(command, syncContext).ConfigureAwait(false);
            }
            // optimization: single item read requires no parsing of Response message
            var entities        = new EntityValue[1];
            var key             = keys[0];
            var id              = key.AsString();
            var partitionKey    = new PartitionKey(id);
            // todo handle error;
            using (var pooled   = syncContext.EntityProcessor.Get())
            using (var response = await cosmosContainer.ReadItemStreamAsync(id, partitionKey).ConfigureAwait(false)) {
                var processor   = pooled.instance;
                var content     = response.Content;
                if (content == null) {
                    entities[0] = new EntityValue(key);
                } else {
                    var     buffer      = new StreamBuffer();
                    var     payload     = await EntityUtils.ReadToEndAsync(content, buffer).ConfigureAwait(false);
                    payload             = EntityUtils.CreateCopy(payload, syncContext.MemoryBuffer);
                    bool    asIntKey    = command.isIntKey == true; 
                    var     json        = processor.ReplaceKey(payload, "id", asIntKey, command.keyName, out _, out _);
                    entities[0]         = new EntityValue(key, json);
                }
            }
            return new ReadEntitiesResult{entities = entities };
        }
        
        private async Task<ReadEntitiesResult> ReadManyEntities(ReadEntities command, SyncContext syncContext) {
            var keys            = command.ids;
            var entities        = new EntityValue[keys.Count];
            var destEntities    = new List<EntityValue>(keys.Count);
            var list            = new List<(string, PartitionKey)>(keys.Count);
            foreach (var key in keys) {
                var id = key.AsString();
                list.Add((id, new PartitionKey(id)));
            }
            // todo handle error;
            using (var response = await cosmosContainer.ReadManyItemsStreamAsync(list).ConfigureAwait(false))
            using (var pooled   = syncContext.ObjectMapper.Get()) {
                var buffer      = new StreamBuffer();
                var reader      = pooled.instance.reader;
                var documents   = await CosmosUtils.ReadDocuments(reader, response.Content, buffer).ConfigureAwait(false);
                EntityUtils.CopyEntities(documents, "id", command.isIntKey, command.keyName, destEntities, syncContext);
                var entitiesMap = new Dictionary<JsonKey, EntityValue>(destEntities.Count, JsonKey.Equality);
                foreach (var entityValue in destEntities) {
                    entitiesMap.Add(entityValue.key, entityValue);
                }
                for (int n = 0; n < keys.Count; n++) {
                    var key = keys[n];
                    if (entitiesMap.TryGetValue(key, out var value)) {
                        entities[n] = value;
                    } else {
                        entities[n] = new EntityValue(key);
                    }
                }
            }
            return new ReadEntitiesResult{ entities = entities };
        }

        private readonly bool filterByClient = false; // true: used for development => query all and filter thereafter
        
        public override async Task<QueryEntitiesResult> QueryEntitiesAsync(QueryEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var buffer      = new StreamBuffer();
            var documents   = new List<JsonValue>();
            var sql         = filterByClient ? null : "SELECT * FROM c WHERE " + command.GetFilter().query.Cosmos;
            if (command.limit != null) {
                sql += $" OFFSET 0 LIMIT {command.limit}";
            }
            QueryRequestOptions queryOptions = null;
            if (command.maxCount != null) {
                queryOptions = new QueryRequestOptions { MaxItemCount = command.maxCount };
            }
            string continuationToken = null;
            using (FeedIterator iterator    = cosmosContainer.GetItemQueryStreamIterator(sql, command.cursor, queryOptions))
            using (var pooled               = syncContext.ObjectMapper.Get()) {
                while (iterator.HasMoreResults) {
                    using(ResponseMessage response = await iterator.ReadNextAsync().ConfigureAwait(false)) {
                        if (response.StatusCode != HttpStatusCode.OK) {
                            var error = new TaskExecuteError(TaskErrorType.ValidationError, response.ErrorMessage);
                            return new QueryEntitiesResult { Error = error };
                        }
                        var reader  = pooled.instance.reader;
                        var docs    = await CosmosUtils.ReadDocuments(reader, response.Content, buffer).ConfigureAwait(false);
                        if (docs == null)
                            throw new InvalidOperationException($"no Documents in Cosmos ResponseMessage. command: {command}");
                        documents.AddRange(docs);
                        continuationToken = response.ContinuationToken;
                        if (continuationToken != null) {
                            break;
                        }
                    }
                }
            }
            var entities    = new List<EntityValue>(documents.Count);
            EntityUtils.CopyEntities(documents, "id", command.isIntKey, command.keyName, entities, syncContext);
            if (filterByClient) {
                throw new NotImplementedException();
                // return FilterEntities(command, entities, syncContext);
            }
            return new QueryEntitiesResult{ entities = entities.ToArray(), cursor = continuationToken};
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntitiesAsync (AggregateEntities command, SyncContext syncContext) {
            var sql     = "SELECT  VALUE COUNT(0) FROM c";
            var filter  = command.GetFilter();
            if (!filter.IsTrue) {
                sql += $" WHERE {filter.query.Cosmos}";
            }
            var query           = new QueryDefinition (sql);
            var queryIterator   = cosmosContainer.GetItemQueryIterator<int>(query);
            foreach (int count in await queryIterator.ReadNextAsync()) {
                var result = new AggregateEntitiesResult { container = command.container, value = count };
                return result;
            }
            return new AggregateEntitiesResult { Error = new TaskExecuteError($"aggregate {command.type} - unexpected query result" ) };
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntitiesAsync(DeleteEntities command, SyncContext syncContext) {
            await EnsureContainerExists().ConfigureAwait(false);
            var keys = command.ids;
            foreach (var key in keys) {
                var id              = key.AsString();
                var partitionKey    = new PartitionKey(id);
                // todo handle error;
                using (var _ = await cosmosContainer.DeleteItemStreamAsync(id, partitionKey).ConfigureAwait(false)) {
                }
            }
            return new DeleteEntitiesResult();
        }
    }
}

#endif