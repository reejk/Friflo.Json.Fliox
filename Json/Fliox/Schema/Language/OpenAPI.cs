// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Fliox.Schema.Definition;

// Allowed namespaces: .Schema.Definition, .Schema.Doc, .Schema.Utils
namespace Friflo.Json.Fliox.Schema.Language
{
    public sealed class OpenAPI
    {
        private  readonly   Generator       generator;
        private  readonly   StandardTypes   standardTypes;
        
        private OpenAPI (Generator generator) {
            this.generator  = generator;
            standardTypes   = generator.standardTypes;
        }
        
        public static void Generate(Generator generator) {
            var emitter     = new OpenAPI(generator);
            var schemaType  = generator.FindSchemaType();
            if (schemaType == null)
                return;
            var sb = new StringBuilder();
            emitter.EmitPaths(schemaType, sb);
            var paths       = sb.ToString();
            var link        = $"Generated by <a href='{Generator.Link}'>JSON Fliox</a>";
            var doc         = schemaType.doc != null ? $"{schemaType.doc}\\n\\n{link}" : link;  
            var description = JsonSchemaGenerator.GetDoc("", doc, "");
            var tags        = CreateTags (schemaType);
            var info        = schemaType.SchemaInfo;
            var contact     = GetContact(info);
            var license     = GetLicense(info);
            var servers     = GetServers(info, generator.databaseUrl);
            var tos         = info?.termsOfService == null ? "" : $@",
    ""termsOfService"": ""{info.termsOfService}""";
            var api     = $@"
{{
  ""openapi"": ""3.0.0"",
  ""x-generator"": ""{Generator.Note}"",
  ""info"": {{
    ""title"":        ""{schemaType.Name}"",
    ""description"":  {description},
    ""version"":      ""{info?.version ?? "0.0.0"}""{tos}{contact}{license}
  }},
  {servers},
  ""tags"": [{tags}],
  ""paths"": {{{paths}
  }}   
}}";
            generator.files.Add("openapi.json", api);
        }
        
        private static string GetContact (SchemaInfo info) {
            if (info == null)
                return "";
            var sb = new StringBuilder();
            var parent = ",\n    \"contact\": {\n";
            Property(parent, "name",    info.contactName,   sb);
            Property(parent, "url",     info.contactUrl,    sb);
            Property(parent, "email",   info.contactEmail,  sb);
            if (sb.Length == 0)
                return "";
            sb.Append("\n    }");
            return sb.ToString();
        }
        
        private static string GetLicense (SchemaInfo info) {
            if (info == null)
                return "";
            var sb = new StringBuilder();
            var parent = ",\n    \"license\": {\n";
            Property(parent, "name",    info.licenseName,   sb);
            Property(parent, "url",     info.licenseUrl,    sb);
            if (sb.Length == 0)
                return "";
            sb.Append("\n    }");
            return sb.ToString();
        }
        
        private static string GetServers (SchemaInfo info, string databaseUrl) {
            var sb          = new StringBuilder();
            var allServers  = new List<SchemaInfoServer>();
            if (databaseUrl != null) {
                allServers.Add(new SchemaInfoServer(databaseUrl, "local server" ));
            }
            if (info?.servers != null) {
                allServers.AddRange(info.servers);
            }
            sb.Append("\"servers\": [\n");
            bool isFirst = true;
            foreach (var server in allServers) {
                if (isFirst)
                    isFirst = false;
                else
                    sb.Append(",\n");
                var sbServer = new StringBuilder();
                GetServer(server, sbServer);
                sb.Append(sbServer);
            }
            if (sb.Length == 0)
                return "";
            sb.Append("\n  ]");
            return sb.ToString();
        }
        
        private static void GetServer (SchemaInfoServer server, StringBuilder sb) {
            var parent = "    {\n";
            Property(parent, "description", server.description, sb);
            Property(parent, "url",         server.url,         sb);
            if (sb.Length == 0)
                return;
            sb.Append("\n    }");
        }
        
        private static void Property (string parent, string name, string value, StringBuilder sb) {
            if (value == null)
                return;
            if (sb.Length == 0)
                sb.Append(parent);
            else
                sb.Append(",\n");
            sb.Append("      \"");
            sb.Append(name);
            sb.Append("\": \"");
            sb.Append(value);
            sb.Append("\"");
        }
        
        private const string StringType     = @"""type"": ""string""";
        private const string IntegerType    = @"""type"": ""integer""";
        private const string BooleanType    = @"""type"": ""boolean""";
        private const string JsonValueType  = @" ";
        private const string JsonKeyType    = @"""type"": ""string""";
        private const string JsonStringType = @"""type"": ""string""";
        
        private static string CreateTags(TypeDef schemaType) {
            var sb = new StringBuilder();
            var anchorAttr = $"target='{schemaType.Name}'";
            sb.Append($@"
    {{
      ""name"": ""database"",
      ""description"": ""<a {anchorAttr} href='html/schema.html'>schema</a>""
    }},
    {{
      ""name"": ""commands"",
      ""description"": ""database <a {anchorAttr} href='html/schema.html#commands'>commands</a>""
    }}");
            foreach (var container in schemaType.Fields) {
                var type = container.type;
                var link = $"{type.Namespace}.{type.Name}";
                sb.Append($@",
    {{
      ""name"": ""{container.name}"",
      ""description"": ""entity type: <a {anchorAttr} href='html/schema.html#{link}'>{type.Name}</a>""
    }}");
            }
            return sb.ToString();
        }
        
        private string GetType (TypeDef typeDef) {
            if (typeDef == standardTypes.String)
                return StringType;
            if (typeDef == standardTypes.Boolean)
                return BooleanType;
            if (typeDef == standardTypes.JsonValue)
                return JsonValueType;
            if (typeDef == standardTypes.JsonEntity)
                return JsonValueType;
            if (typeDef == standardTypes.JsonKey)
                return JsonKeyType;
            if (typeDef == standardTypes.ShortString)
                return JsonStringType;

            return Ref (typeDef, true, generator);
        }
        
        private string GetTypeRef (string @namespace, string name) {
            var typeDef = generator.FindTypeDef(@namespace, name);
            return Ref (typeDef, true, generator);
        }
        
        private void EmitPaths(TypeDef schemaType, StringBuilder sb) {
            var dbContainers    = GetTypeRef("Friflo.Json.Fliox.Hub.DB.Cluster", "DbContainers");
            var response        = new ContentRef(dbContainers, false);
            EmitPath("database", "get", "/",   null, "return all database containers", null, response, sb);

            EmitMessages("cmd", schemaType.Commands, sb);
            EmitMessages("msg", schemaType.Messages, sb);
            foreach (var container in schemaType.Fields) {
                EmitContainerApi(container, sb);
            }
        }
        
        private void EmitMessages(string messageType, IReadOnlyList<MessageDef> messages, StringBuilder sb) {
            if (messages == null)
                return;
            foreach (var message in messages) {
                EmitMessage(message, messageType, sb);
            }
        }
        
        private void EmitMessage(MessageDef type, string messageType, StringBuilder sb) {
            // var queryParams = new List<Parameter>();
            Content request = null;
            var paramType   = type.param?.type;
            if (paramType != null) {
                var paramRef    = GetType(paramType);
                request         = new ContentRef(paramRef, false);
                // queryParams.Add(new Parameter("query", "param", paramRef, false));
            }
            var doc             = type.doc ?? "";
            var tag             = messageType == "cmd" ? "commands" : "messages";
            var resultType      = type.result?.type;
            Content response;
            if (resultType != null) {
                var resultRef   = GetType(resultType); 
                response        = new ContentRef(resultRef, false);
            } else {
                response        = new ContentRef("", false);
            }
            EmitPath(tag, "post", $"/?{messageType}={type.name}", null, doc, request, response, sb);
        }
        
        private void EmitContainerApi(FieldDef container, StringBuilder sb) {
            var name    = container.name;
            var typeRef = Ref (container.type, true, generator);
            EmitPathContainer (name, $"/{name}",              typeRef, sb);
            
            EmitPathId        (name, $"/{name}/{{id}}",       typeRef, sb);

            var bulkGetResponse = new ContentRef(typeRef, true);
            EmitPath (name, "post", $"/{name}/bulk-get",    null, $"get multiple records by id from container {container}",
                new ContentRef(StringType, true), bulkGetResponse, sb);
            
            var bulkDeleteResponse = new ContentText();
            EmitPath (name, "post", $"/{name}/bulk-delete", null, $"delete multiple records by id in container {container}",
                new ContentRef(StringType, true), bulkDeleteResponse, sb);
        }
        
        private static void AppendPath(string path, string methods, StringBuilder sb) {
            if (sb.Length > 0)
                sb.Append(",");
            sb.Append($@"
    ""{path}"": {{");
            sb.Append(methods);
            sb.Append($@"
    }}");
        }
        
        private static void EmitPathContainer(string container, string path, string typeRef, StringBuilder sb) {
            var methodSb = new StringBuilder();
            var getParams = new [] {
                new Parameter("query",  "filter",   StringType,  false, "filter returned records by applying a expression predicate. E.g. `o.name == 'Peter'`"),
                new Parameter("query",  "limit",    IntegerType, false, "limit the number of returned records"),
                new Parameter("query",  "maxCount", IntegerType, false, "maximum number of records. Result will return a **cursor** if more records available."),
                new Parameter("query",  "cursor",   StringType,  false, "pass the **cursor** returned by the previous request")
            };
            EmitMethod(container, "get",    $@"return / filter multiple records from container {container}", 
                "To process big result sets fetch them iteratively by setting **maxCount** of records per request and use the returned **cursor** on the subsequent request.",
                null, new ContentRef(typeRef, false), getParams, methodSb);
            EmitMethod(container, "put",    $"create or update multiple records in container {container}", null,
                new ContentRef(typeRef, true), new ContentText(), null, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitPathId(string container, string path, string typeRef, StringBuilder sb) {
            var methodSb    = new StringBuilder();
            var bodyContent = new ContentRef(typeRef, false);
            var idParam     = new [] { new Parameter("path", "id", StringType, true, null)};
            var patchType = @"
                    ""type"": ""object"",
                    ""properties"": {
                      ""op"": {
                        ""enum"": [""replace""]
                      },
                      ""path"": {
                        ""type"": ""string""
                      },
                      ""value"": { }
                    }";
            var patchExample = @"[
                  {
                    ""op"":    ""replace"",
                    ""path"":  "".name"",
                    ""value"": ""Hello Patch!""
                  }
                ]";
            var patchBody = new ContentRef(patchType, true, patchExample);
            EmitMethod (container, "get",    $"get a single record from container {container}", null, null, bodyContent,              idParam, methodSb);
            EmitMethod (container, "put",    $"write a single record to container {container}", null, bodyContent, new ContentText(), idParam, methodSb);
            EmitMethod (container, "patch",  $"patch a single record in container {container}", null, patchBody,   new ContentText(), idParam, methodSb);
            EmitMethod (container, "delete", $"delete a single record in container {container} by id", null, null, new ContentText(), idParam, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitPath(
            string                  tag,
            string                  method,
            string                  path,
            ICollection<Parameter>  queryParams,
            string                  summary,
            Content                 request,
            Content                 response,
            StringBuilder           sb)
        {
            var methodSb = new StringBuilder();
            EmitMethod(tag, method,   summary, null, request, response, queryParams, methodSb);
            AppendPath(path, methodSb.ToString(), sb);
        }
        
        private static void EmitMethod(
            string                  tag,
            string                  method,
            string                  summary,
            string                  description,
            Content                 request,
            Content                 response,
            ICollection<Parameter>  queryParams,
            StringBuilder sb)
        {
            if (sb.Length > 0)
                sb.Append(",");
            var querySb         = new StringBuilder();
            var summaryStr      = JsonSchemaGenerator.GetDoc("", summary, "");
            var queryStr        = "";
            var descriptionStr  = description == null ? "" : $@"
        ""description"":    ""{description}"","; 
            if (queryParams != null) {
                foreach (var queryParam in queryParams) {
                    if (querySb.Length > 0)
                        querySb.Append(",");
                    var param = queryParam.Get();
                    querySb.Append(param);
                }
                queryStr = $@"
        ""parameters"": [{querySb}
        ],";    
            }
            var requestStr = request == null ? "" : $@"
        ""requestBody"": {{          
          ""content"": {request.Get()}
        }},";
            var responseStr = response.Get();
            var methodStr = $@"
      ""{method}"": {{
        ""summary"":    {summaryStr},{descriptionStr}
        ""tags"":       [""{tag}""],{queryStr}{requestStr}
        ""responses"": {{
          ""200"": {{             
            ""description"": ""OK"",
            ""content"": {responseStr}
          }}
        }}
      }}";
            sb.Append(methodStr);
        }
        
        private static string Ref(TypeDef type, bool required, Generator generator) {
            var name        = type.Name;
            var typePath    = type.Path;
            var prefix      = $"{typePath}{generator.fileExt}";
            var refType = $"\"$ref\": \"{prefix}#/definitions/{name}\"";
            if (!required)
                return $"\"oneOf\": [{{ {refType} }}, {{\"type\": \"null\"}}]";
            return refType;
        }
    }
    
    internal sealed class Parameter {
        private     readonly    string  paramType;
        private     readonly    string  name;
        private     readonly    string  type;
        private     readonly    bool    required;
        private     readonly    string  description;
        
        internal Parameter(string paramType, string name, string type, bool required, string description) {
            this.paramType      = paramType;
            this.name           = name;
            this.type           = type;
            this.required       = required;
            this.description    = description;
        }
        
        internal string Get() {
            var requiredStr = required ? @"
            ""required"": true," : "";
            return $@"
          {{
            ""in"":       ""{paramType}"",
            ""name"":     ""{name}"",
            ""schema"":   {{ {type} }},{requiredStr}
            ""description"": ""{description ?? ""}""
          }}";
        }
    }
    
    internal abstract class Content {
        
        internal abstract string Get(); 
    }
    
    internal sealed class ContentText : Content {
      
        internal override string Get() {
            return @"{
              ""text/plain"": { }
            }";
        } 
    }

    internal sealed class ContentRef : Content {
        private    readonly    string   type;
        private    readonly    bool     isArray;
        private    readonly    string   example;
        
        internal ContentRef(string type, bool isArray, string example = null) {
            this.type       = type;
            this.isArray    = isArray;
            this.example    = example;
        }
        
        internal override string Get() {
            var typeStr = isArray ? $@"""type"": ""array"",
                  ""items"": {{ {type} }}" : type;
            var requestExample = example == null ? "" : $@",
                ""example"": {example}";

            return $@"{{
              ""application/json"": {{
                ""schema"": {{
                  {typeStr}
                }}{requestExample}
              }}
            }}";
        }
    }
}