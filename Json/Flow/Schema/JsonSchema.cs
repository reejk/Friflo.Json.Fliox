﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Flow.Schema
{
    public class JsonSchema
    {
        public   readonly   Generator   generator;
        private  readonly   bool        separateEntities;
        private  const      string      Next = ",\r\n";
        
        public JsonSchema (TypeStore typeStore, string stripNamespace, bool separateEntities) {
            generator               = new Generator(typeStore, stripNamespace, ".json");
            this.separateEntities   = separateEntities;
            if (separateEntities) {
                generator.SetPackageNameCallback(type => {
                    // todo add Generator method
                    if (generator.typeMappers.TryGetValue(type, out var mapper)) {
                        if (mapper.GetTypeSemantic() == TypeSemantic.Entity)
                            return $"{type.Namespace}.{type.Name}";
                    }
                    return type.Namespace;
                });
            }
        }
        
        public void GenerateSchema() {
            var sb = new StringBuilder();
            // emit custom types
            foreach (var pair in generator.typeMappers) {
                var mapper = pair.Value;
                sb.Clear();
                var result = EmitType(mapper, sb);
                if (result == null)
                    continue;
                generator.AddEmitType(result);
            }
            sb.AppendLine("}");
            generator.GroupTypesByPackage();
            EmitPackageHeaders(sb);
            EmitPackageFooters(sb);

            generator.CreateFiles(sb, ns => $"{ns}{generator.fileExt}", Next); // $"{ns.Replace(".", "/")}.ts");
        }
        
        private EmitType EmitType(TypeMapper mapper, StringBuilder sb) {
            var semantic= mapper.GetTypeSemantic();
            var imports = new HashSet<Type>(); 
            var context = new TypeContext (generator, imports, mapper);
            mapper      = mapper.GetUnderlyingMapper();
            var type    = Generator.GetType(mapper);
            if (type == typeof(BigInteger)) {
                sb.AppendLine("        \"BigInteger\": {");
                sb.AppendLine("            \"type\": \"string\"");
                sb.Append    ("        }");
                return new EmitType(type, semantic, generator, sb, new HashSet<Type>());
            }
            if (mapper.IsComplex) {
                var fields          = mapper.propFields.fields;
                int maxFieldName    = fields.MaxLength(field => field.jsonName.Length);
                
                string  discriminator = null;
                var     discriminant = mapper.discriminant;
                if (discriminant != null) {
                    var baseMapper  = generator.GetPolymorphBaseMapper(type);
                    discriminator   = baseMapper.instanceFactory.discriminator;
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var instanceFactory = mapper.instanceFactory;
                sb.AppendLine($"        \"{type.Name}\": {{");
                if (instanceFactory == null) {
                    sb.AppendLine($"            \"type\": \"object\",");
                } else {
                    sb.AppendLine($"            \"oneOf\": [");
                    bool firstElem = true;
                    foreach (var polyType in instanceFactory.polyTypes) {
                        Generator.Delimiter(sb, Next, ref firstElem);
                        sb.Append($"                {{ {Ref(polyType.type, context)} }}");
                    }
                    sb.AppendLine();
                    sb.AppendLine($"            ],");
                }
                sb.AppendLine($"            \"properties\": {{");
                bool firstField = true;
                var required = new List<string>();
                if (discriminant != null) {
                    var indent = Generator.Indent(maxFieldName, discriminator);
                    sb.Append($"                \"{discriminator}\":{indent} {{ \"enum\": [\"{discriminant}\"] }}");
                    firstField = false;
                    required.Add(discriminator);
                }
                // fields
                foreach (var field in fields) {
                    var fieldType = GetFieldType(field.fieldType, context, out var isOptional);
                    var indent = Generator.Indent(maxFieldName, field.jsonName);
                    if (field.required || !isOptional)
                        required.Add(field.jsonName);
                    Generator.Delimiter(sb, Next, ref firstField);
                    sb.Append($"                \"{field.jsonName}\":{indent} {{ {fieldType} }}");
                }
                sb.AppendLine();
                sb.AppendLine("            },");
                if (required.Count > 0 ) {
                    bool firstReq = true;
                    sb.AppendLine("            \"required\": [");
                    foreach (var item in required) {
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
                var enumValues = mapper.GetEnumValues();
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
                return new EmitType(type, semantic, generator, sb, new HashSet<Type>());
            }
            return null;
        }
        
        // Note: static by intention
        private static string GetFieldType(TypeMapper mapper, TypeContext context, out bool isOptional) {
            mapper      = mapper.GetUnderlyingMapper();
            isOptional  = mapper.isNullable;
            var type    = Generator.GetType(mapper);
            if (type == typeof(JsonValue)) {
                return "\"type\": \"object\"";
            }
            if (type == typeof(string)) {
                return "\"type\": \"string\"";
            }
            if (type == typeof(DateTime)) {
                return "\"type\": \"string\", \"format\": \"date-time\"";
            }
            if (type == typeof(bool)) {
                return "\"type\": \"boolean\"";
            }
            if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                || type == typeof(float) || type == typeof(double)) {
                return "\"type\": \"number\"";
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var elementTypeName = GetFieldType(elementMapper, context, out _);
                return $"\"type\": \"array\", \"items\": {{ {elementTypeName} }}";
            }
            var isDictionary = type.GetInterfaces().Contains(typeof(IDictionary));
            if (isDictionary) {
                var valueMapper = mapper.GetElementMapper();
                var valueTypeName = GetFieldType(valueMapper, context, out _);
                return $"\"type\": \"object\", \"additionalProperties\": {{ {valueTypeName} }}";
            }
            context.imports.Add(type);
            return Ref(type, context);
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                var package = pair.Value;
                sb.Clear();
                sb.AppendLine("{");
                sb.AppendLine("    \"$schema\": \"http://json-schema.org/draft-07/schema#\",");
                sb.AppendLine("    \"$comment\": \"Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema\",");
                if (separateEntities) {
                    var first = package.emitTypes.FirstOrDefault();
                    if (first != null && first.semantic == TypeSemantic.Entity) {
                        var entityName = first.type.Name;
                        sb.AppendLine($"    \"$ref\": \"#/definitions/{entityName}\",");
                    }
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
        
        private static string Ref(Type type, TypeContext context) {
            var name = type.Name;
            // if (generator.IsUnionType(type))
            //    name = $"{type.Name}_Union";
            var generator       = context.generator;
            var typePackage     = generator.GetPackageName(type);
            var ownerPackage    = generator.GetPackageName(context.owner.type);
            bool samePackage    = typePackage == ownerPackage;
            var prefix          = samePackage ? "" : $"./{typePackage}{generator.fileExt}";
            return $"\"$ref\": \"{prefix}#/definitions/{name}\"";
        }
    }
}