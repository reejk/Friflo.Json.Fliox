﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Schema.Language;

// Note! - Must not have any dependency to System.Net or System.Net.Http (or other HTTP stuff)
namespace Friflo.Json.Fliox.Hub.Remote
{
    public delegate byte[] CreateZip(IDictionary<string, string> files);

    internal sealed class SchemaHandler : IRequestHandler
    {
        private   const     string                              SchemaBase = "/schema";
        internal            string                              image = "img/Json-Fliox-53x43.svg";
        internal  readonly  CreateZip                           zip;
        private   readonly  Dictionary<string, SchemaResource>  schemas         = new Dictionary<string, SchemaResource>();
        private   readonly  List<CustomGenerator>               generators      = new List<CustomGenerator>();
        private             string                              cacheControl    = HttpHostHub.DefaultCacheControl;
        
        internal            ICollection<CustomGenerator>        Generators      => generators;

        internal SchemaHandler() {
            this.zip = ZipUtils.Zip;
        }
        
        public SchemaHandler CacheControl(string cacheControl) {
            this.cacheControl   = cacheControl;
            return this;
        }
        
        public string[]  Routes => new []{ SchemaBase };

        public bool IsMatch(RequestContext context) {
            if (context.method != "GET")
                return false;
            return RequestContext.IsBasePath(SchemaBase, context.route);
        }
        
        public Task HandleRequest(RequestContext context) {
            if (context.route.Length == SchemaBase.Length) {
                context.WriteError("invalid schema path", "missing database", 400);
                return Task.CompletedTask;
            }
            var hub         = context.hub;
            var route       = context.route.Substring(SchemaBase.Length + 1);
            var firstSlash  = route.IndexOf('/');
            var name        = firstSlash == -1 ? route : route.Substring(0, firstSlash);
            var schema      = GetSchemaResource(hub, name, out string error);
            if (schema == null) {
                context.WriteError(error, name, 404);
                return Task.CompletedTask;
            }
            var schemaPath  = route.Substring(firstSlash + 1);
            var result      = schema.GetSchemaFile(schemaPath, this, context);
            if (!result.success) {
                context.WriteError("schema error", result.content, 404);
                return Task.CompletedTask;
            }
            if (cacheControl != null) {
                context.AddHeader("Cache-Control", cacheControl); // seconds
            }
            if (result.isText) {
                context.WriteString(result.content, result.contentType, 200);
                return Task.CompletedTask;
            }
            context.Write(result.bytes, 0, result.contentType, 200);
            return Task.CompletedTask;
        }
        
        private SchemaResource GetSchemaResource(FlioxHub hub, string name, out string error) {
            if (schemas.TryGetValue(name, out var schema)) {
                error = null;
                return schema;
            }
            if (!hub.TryGetDatabase(name, out var database)) {
                error = "schema not found";
                return null;
            }
            var typeSchema = database.Schema.typeSchema;
            if (typeSchema == null) {
                error = "missing schema for database";
                return null;
            }
            error = null;
            return AddSchema(name, typeSchema);
        }
        
        internal SchemaResource AddSchema(string name, TypeSchema typeSchema, ICollection<TypeDef> sepTypes = null) {
            sepTypes    = sepTypes ?? typeSchema.GetEntityTypes().Values;
            var schema  = new SchemaResource(name, typeSchema, sepTypes);
            schemas.Add(name, schema);
            return schema;
        }
        
        internal void AddGenerator(string type, string name, SchemaGenerator schemaGenerator) {
            if (name == null) throw new NullReferenceException(nameof(name));
            var generator = new CustomGenerator(type, name, schemaGenerator);
            generators.Add(generator);
        }
    }
    
    internal class ModelResource {
        internal  readonly  SchemaModel     schemaModel;
        internal  readonly  string          zipNameSuffix;  // .csharp.zip, json-schema.zip, ...
        private             byte[]          zipArchive;
        internal  readonly  JsonValue       fullSchema;

        public    override  string          ToString() => schemaModel.type;

        internal ModelResource(SchemaModel schemaModel, JsonValue fullSchema) {
            this.schemaModel    = schemaModel;
            this.fullSchema     = fullSchema;
            zipNameSuffix       = $".{schemaModel.type}.zip";
        }
        
        internal byte[] GetZipArchive (CreateZip zip) {
            if (zipArchive == null && zip != null ) {
                zipArchive = zip(schemaModel.files);
            }
            return zipArchive;
        }
    }

    internal readonly struct Result {
        internal  readonly  bool        success;
        internal  readonly  string      content;
        internal  readonly  string      contentType;
        internal  readonly  JsonValue   bytes;
        internal  readonly  bool        isText;
        
        private Result (string content, string contentType, JsonValue bytes, bool isText, bool success) {
            this.content        = content;
            this.contentType    = contentType;
            this.bytes          = bytes;
            this.isText         = isText;
            this.success        = success;
        }
        
        internal static Result Success(string  content, string  contentType) {
            return new Result(content, contentType, default, true, true);
        }
        
        internal static  Result Success(JsonValue  content, string  contentType) {
            return new Result(null, contentType, content, false, true);
        }
        
        internal static Result Error(string  content) {
            return new Result(content, "text/plain", default, true, false);
        }
    }
}