﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Sync;
using Friflo.Json.Fliox.Mapper;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.DB.NoSQL.Remote
{
    public class RemoteHostDatabase : EntityDatabase
    {
        internal readonly   EntityDatabase  local;
        /// Only set to true for testing. It avoids an early out at <see cref="Event.EventSubscriber.SendEvents"/> 
        public              bool            fakeOpenClosedSockets;

        public RemoteHostDatabase(EntityDatabase local) {
            this.local = local;
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            EntityContainer localContainer = local.CreateContainer(name, local);
            RemoteHostContainer container = new RemoteHostContainer(name, this, localContainer);
            return container;
        }
        
        public override async Task<SyncResponse> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            var result = await local.ExecuteSync(syncRequest, messageContext).ConfigureAwait(false);
            result.reqId = syncRequest.reqId;
            return result;
        }

        public async Task<JsonResponse> ExecuteRequestJson(JsonUtf8 jsonRequest, MessageContext messageContext) {
            try {
                JsonUtf8 jsonResponse;
                using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                    ObjectMapper    mapper  = pooledMapper.instance;
                    ObjectReader    reader  = mapper.reader;
                    ProtocolRequest request = ReadRequest (reader, jsonRequest, out string error);
                    if (request == null)
                        return JsonResponse.CreateResponseError(messageContext, error, ResponseStatusType.Error);
                    ProtocolResponse response = await ExecuteRequest(request, messageContext).ConfigureAwait(false);
                    mapper.WriteNullMembers = false;
                    mapper.Pretty = true;
                    jsonResponse = CreateResponse(mapper.writer, response);
                }
                return new JsonResponse(jsonResponse, ResponseStatusType.Ok);
            } catch (Exception e) {
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                return JsonResponse.CreateResponseError(messageContext, errorMsg, ResponseStatusType.Exception);
            }
        }
        
        /// Caller need to check <see cref="reader"/> error state. 
        private static ProtocolRequest ReadRequest (ObjectReader reader, JsonUtf8 jsonRequest, out string error) {
            var msg = reader.Read<ProtocolMessage>(jsonRequest);
            if (reader.Error.ErrSet) {
                error = reader.Error.msg.AsString();
                return null;
            }
            if (msg is ProtocolRequest req) {
                error = null;
                return req;
            }
            error = $"Expected database request. Was: MessageType: {msg.MessageType}";
            return null;
        }
        
        private static JsonUtf8 CreateResponse (ObjectWriter writer, ProtocolResponse response) {
            return new JsonUtf8(writer.WriteAsArray<ProtocolMessage>(response));
        }
        
        private async Task<ProtocolResponse> ExecuteRequest(ProtocolRequest request, MessageContext messageContext) {
            switch (request.MessageType) {
                case MessageType.sync:
                    return await ExecuteSync((SyncRequest)request, messageContext).ConfigureAwait(false);
                default:
                    throw new NotImplementedException();
            }
        }
    }
    
    public enum ResponseStatusType {
        /// maps to HTTP 200 OK
        Ok,         
        /// maps to HTTP 400 Bad Request
        Error,
        /// maps to HTTP 500 Internal Server Error
        Exception
    }
    
    public class JsonResponse
    {
        public readonly     JsonUtf8            body;
        public readonly     ResponseStatusType  statusType;
        
        public JsonResponse(JsonUtf8 body, ResponseStatusType statusType) {
            this.body       = body;
            this.statusType  = statusType;
        }
        
        public static JsonResponse CreateResponseError(MessageContext messageContext, string message, ResponseStatusType type) {
            var errorResponse = new ErrorResponse {message = message};
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                ObjectMapper mapper = pooledMapper.instance;
                var bodyArray = mapper.WriteAsArray<ProtocolMessage>(errorResponse);
                var body = new JsonUtf8(bodyArray);
                return new JsonResponse(body, type);
            }
        }
    }
    
    public class RemoteHostContainer : EntityContainer
    {
        private readonly    EntityContainer local;
        
        public  override    bool            Pretty       => local.Pretty;

        public RemoteHostContainer(string name, EntityDatabase database, EntityContainer localContainer)
            : base(name, database) {
            local = localContainer;
        }


        public override async Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            return await local.CreateEntities(command, messageContext).ConfigureAwait(false);
        }

        public override async Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            return await local.UpsertEntities(command, messageContext).ConfigureAwait(false);
        }

        public override async Task<ReadEntitiesResult> ReadEntities(ReadEntities command, MessageContext messageContext) {
            return await local.ReadEntities(command, messageContext).ConfigureAwait(false);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            return await local.QueryEntities(command, messageContext).ConfigureAwait(false);
        }
        
        public override async Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            return await local.DeleteEntities(command, messageContext).ConfigureAwait(false);
        }
    }
}
