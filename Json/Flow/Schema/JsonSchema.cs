﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Flow.Schema
{
    public class JsonSchema
    {
        public   readonly   Generator                   generator;
        private  const      string                      Next = ",\r\n";
        private  readonly   Dictionary<ITyp, string>    standardTypes;

        
        public JsonSchema (TypeStore typeStore, ICollection<string> stripNamespaces, ICollection<Type> separateTypes) {
            generator               = new Generator(typeStore, stripNamespaces, ".json", separateTypes);
            standardTypes = GetStandardTypes(generator.system);
        }
        
        public void GenerateSchema() {
            var sb = new StringBuilder();
            // emit custom types
            foreach (var pair in generator.typeMappers) {
                var type = pair.Key;
                sb.Clear();
                var result = EmitType(type, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            generator.GroupTypesByPackage(false);
            EmitPackageHeaders(sb);
            EmitPackageFooters(sb);
            generator.CreateFiles(sb, ns => $"{ns}{generator.fileExt}", Next); // $"{ns.Replace(".", "/")}.ts");
        }
        
        private static Dictionary<ITyp, string> GetStandardTypes(ITypeSystem system) {
            var map = new Dictionary<ITyp, string>() {
                { system.Unit8,         "\"type\": \"number\", \"minimum\": 0, \"maximum\": 255" },
                { system.Int16,         "\"type\": \"number\", \"minimum\": -32768, \"maximum\": 32767" },
                { system.Int32,         "\"type\": \"number\", \"minimum\": -2147483648, \"maximum\": 2147483647" },
                { system.Int64,         "\"type\": \"number\", \"minimum\": -9223372036854775808, \"maximum\": 9223372036854775807" },
                
                { system.Double,        "\"type\": \"number\"" },
                { system.Float,         "\"type\": \"number\"" },
                
                { system.BigInteger,    "\"type\": \"string\", \"pattern\": \"^-?[0-9]+$\"" }, // https://www.regextester.com/
                { system.DateTime,      "\"type\": \"string\", \"format\": \"date-time\"" }
            };
            return map;
        }
        
        private EmitType EmitStandardType(ITyp type, StringBuilder sb, Generator generator) {
            if (!standardTypes.TryGetValue(type, out var definition))
                return null;
            var typeName = generator.GetTypeName(type);
            sb.AppendLine($"        \"{typeName}\": {{");
            sb.AppendLine($"            {definition}");
            sb.Append    ( "        }");
            return new EmitType(type, TypeSemantic.None, generator, sb);
        }
        
        private EmitType EmitType(ITyp type, StringBuilder sb) {
            var semantic= type.TypeSemantic;
            var imports = new HashSet<ITyp>(); 
            var context = new TypeContext (generator, imports, type);
            // mapper      = mapper.GetUnderlyingMapper();
            // type        = mapper.GetUnderlyingMapper();
            var standardType = EmitStandardType(type, sb, generator);
            if (standardType != null ) {
                return standardType;
            }
            if (type.IsComplex) {
                var fields          = type.Fields;
                int maxFieldName    = fields.MaxLength(field => field.jsonName.Length);
                
                string  discriminator = null;
                var     discriminant = type.Discriminant;
                if (discriminant != null) {
                    var baseMapper  = type.BaseType;
                    discriminator   = baseMapper.InstanceFactory.discriminator;
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var instanceFactory = mapper.InstanceFactory;
                sb.AppendLine($"        \"{type.Name}\": {{");
                if (instanceFactory == null) {
                    sb.AppendLine($"            \"type\": \"object\",");
                } else {
                    sb.AppendLine($"            \"oneOf\": [");
                    bool firstElem = true;
                    foreach (var polyType in instanceFactory.polyTypes) {
                        Generator.Delimiter(sb, Next, ref firstElem);
                        sb.Append($"                {{ {Ref(polyType.type, false, context)} }}");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"            ],");
                }
                sb.AppendLine($"            \"properties\": {{");
                bool firstField = true;
                var requiredFields = new List<string>();
                if (discriminant != null) {
                    var indent = Generator.Indent(maxFieldName, discriminator);
                    sb.Append($"                \"{discriminator}\":{indent} {{ \"enum\": [\"{discriminant}\"] }}");
                    firstField = false;
                    requiredFields.Add(discriminator);
                }
                // fields
                foreach (var field in fields) {
                    // if (generator.IsDerivedField(type, field))  JSON Schema list all properties
                    //    continue;
                    bool isOptional = !field.required;
                    var fieldType = GetFieldType(field.fieldType, context, ref isOptional);
                    var indent = Generator.Indent(maxFieldName, field.jsonName);
                    if (!isOptional)
                        requiredFields.Add(field.jsonName);
                    Generator.Delimiter(sb, Next, ref firstField);
                    sb.Append($"                \"{field.jsonName}\":{indent} {{ {fieldType} }}");
                }
                sb.AppendLine();
                sb.AppendLine("            },");
                if (requiredFields.Count > 0 ) {
                    bool firstReq = true;
                    sb.AppendLine("            \"required\": [");
                    foreach (var item in requiredFields) {
                        Generator.Delimiter(sb, Next, ref firstReq);
                        sb.Append ($"                \"{item}\"");
                    }
                    sb.AppendLine();
                    sb.AppendLine("            ],");
                }
                var additionalProperties = instanceFactory != null ? "true" : "false"; 
                sb.AppendLine($"            \"additionalProperties\": {additionalProperties}");
                sb.Append     ("        }");
                return new EmitType(type, semantic, generator, sb, imports);
            }
            if (type.IsEnum) {
                var enumValues = type.GetEnumValues();
                sb.AppendLine($"        \"{type.Name}\": {{");
                sb.AppendLine($"            \"enum\": [");
                bool firstValue = true;
                foreach (var enumValue in enumValues) {
                    Generator.Delimiter(sb, Next, ref firstValue);
                    sb.Append($"                \"{enumValue}\"");
                }
                sb.AppendLine();
                sb.AppendLine("            ]");
                sb.Append    ("        }");
                return new EmitType(type, semantic, generator, sb);
            }
            return null;
        }
        
        // Note: static by intention
        private static string GetFieldType(ITyp type, TypeContext context, ref bool isOptional) {
            var system  = context.generator.system;
            isOptional  = isOptional && type.IsNullable;
            // mapper      = mapper.GetUnderlyingMapper();
            // var type    = Generator.GetType(mapper);
            if (type == system.JsonValue) {
                return ""; // allow any type
            }
            if (type == system.String) {
                return $"\"type\": {Opt(isOptional, "string")}";
            }
            if (type == system.Boolean) {
                return "\"type\": \"boolean\"";
            }
            if (type.IsArray) {
                var elementMapper = type.ElementType;
                bool isOpt = false;
                var elementTypeName = GetFieldType(elementMapper, context, ref isOpt);
                return $"\"type\": {Opt(isOptional, "array")}, \"items\": {{ {elementTypeName} }}";
            }
            var isDictionary = type.IsDictionary;
            if (isDictionary) {
                var valueMapper = type.ElementType;
                bool isOpt = false;
                var valueTypeName = GetFieldType(valueMapper, context, ref isOpt);
                return $"\"type\": \"object\", \"additionalProperties\": {{ {valueTypeName} }}";
            }
            context.imports.Add(type);
            return Ref(type, isOptional, context);
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine("{");
                sb.AppendLine( "    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLine($"    \"$comment\": \"{Generator.Note}\",");
                var first = package.emitTypes.FirstOrDefault();
                if (first != null && generator.separateTypes.Contains(first.type)) {
                    var entityName = first.type.Name;
                    sb.AppendLine($"    \"$ref\": \"#/definitions/{entityName}\",");
                }
                sb.Append    ("    \"definitions\": {");
                package.header = sb.ToString();
            }
        }
        
        private void EmitPackageFooters(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine("}");
                package.footer = sb.ToString();
            }
        }
        
        private static string Ref(ITyp type, bool isOptional, TypeContext context) {
            var generator       = context.generator;
            var name = context.generator.GetTypeName(type);
            // if (generator.IsUnionType(type))
            //    name = $"{type.Name}_Union";
            var typePackage     = generator.GetPackageName(type);
            var ownerPackage    = generator.GetPackageName(context.owner);
            bool samePackage    = typePackage == ownerPackage;
            var prefix          = samePackage ? "" : $"./{typePackage}{generator.fileExt}";
            var refType = $"\"$ref\": \"{prefix}#/definitions/{name}\"";
            if (isOptional)
                return $"\"oneOf\": [{{\"type\": \"null\"}}, {{ {refType} }}]";
            return refType;
        }
        
        private static string Opt (bool isOptional, string name) {
            if (isOptional)
                return $"[\"{name}\", \"null\"]";
            return $"\"{name}\"";
        }
    }
}