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
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    /// The code generator is not used anywhere. Its purpose is only to ensure all required APIs are accessible.
    /// It is a 1:1 copy of <see cref="Typescript"/>.
    public class TypescriptExternal
    {
        public  readonly    Generator  generator;

        public TypescriptExternal (TypeStore typeStore, string stripNamespace) {
            generator = new Generator(typeStore, stripNamespace, ".ts", null);
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
            generator.GroupTypesByPackage();
            EmitPackageHeaders(sb);
            // EmitPackageFooters(sb);  no TS footer
            generator.CreateFiles(sb, ns => $"{ns}{generator.fileExt}"); // $"{ns.Replace(".", "/")}{generator.extension}");
        }
        
        private EmitType EmitType(TypeMapper mapper, StringBuilder sb) {
            var semantic= mapper.GetTypeSemantic();
            var imports = new HashSet<Type>();
            var context = new TypeContext (generator, imports, mapper);
            mapper      = mapper.GetUnderlyingMapper();
            var type    = Generator.GetType(mapper);
            if (type == typeof(DateTime)) {
                sb.AppendLine($"export type DateTime = string;");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb, new HashSet<Type>());
            }
            if (type == typeof(BigInteger)) {
                sb.AppendLine($"export type BigInteger = string;");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb, new HashSet<Type>());
            }
            if (mapper.IsComplex) {
                var fields          = mapper.propFields.fields;
                int maxFieldName    = fields.MaxLength(field => field.jsonName.Length);
                
                string  discriminator = null;
                var     discriminant = mapper.Discriminant;
                var extendsStr = "";
                if (discriminant != null) {
                    var baseMapper  = generator.GetPolymorphBaseMapper(type);
                    discriminator   = baseMapper.InstanceFactory.discriminator;
                    extendsStr = $"extends {baseMapper.type.Name} ";
                    maxFieldName = Math.Max(maxFieldName, discriminator.Length);
                }
                var instanceFactory = mapper.InstanceFactory;
                if (instanceFactory == null) {
                    sb.AppendLine($"export class {type.Name} {extendsStr}{{");
                } else {
                    sb.AppendLine($"export type {type.Name}_Union =");
                    foreach (var polyType in instanceFactory.polyTypes) {
                        sb.AppendLine($"    | {polyType.type.Name}");
                        imports.Add(polyType.type);
                    }
                    sb.AppendLine($";");
                    sb.AppendLine();
                    sb.AppendLine($"export abstract class {type.Name} {extendsStr}{{");
                    sb.AppendLine($"    abstract {instanceFactory.discriminator}:");
                    foreach (var polyType in instanceFactory.polyTypes) {
                        sb.AppendLine($"        | \"{polyType.name}\"");
                    }
                    sb.AppendLine($"    ;");
                }
                if (discriminant != null) {
                    var indent = Generator.Indent(maxFieldName, discriminator);
                    sb.AppendLine($"    {discriminator}{indent}  : \"{discriminant}\";");
                }
                // fields                
                foreach (var field in fields) {
                    if (generator.IsDerivedField(type, field))
                        continue;
                    bool isOptional = !field.required;
                    var fieldType = GetFieldType(field.fieldType, context, ref isOptional);
                    var indent = Generator.Indent(maxFieldName, field.jsonName);
                    var optStr = isOptional ? "?" : " ";
                    var nullStr = isOptional ? " | null" : "";
                    sb.AppendLine($"    {field.jsonName}{optStr}{indent} : {fieldType}{nullStr};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb, imports);
            }
            if (type.IsEnum) {
                var enumValues = mapper.GetEnumValues();
                sb.AppendLine($"export type {type.Name} =");
                foreach (var enumValue in enumValues) {
                    sb.AppendLine($"    | \"{enumValue}\"");
                }
                sb.AppendLine($";");
                sb.AppendLine();
                return new EmitType(type, semantic, generator, sb, new HashSet<Type>());
            }
            return null;
        }
        
        // Note: static by intention
        private static string GetFieldType(TypeMapper mapper, TypeContext context, ref bool isOptional) {
            mapper      = mapper.GetUnderlyingMapper();
            isOptional  = isOptional && mapper.isNullable;
            var type    = Generator.GetType(mapper);
            if (type == typeof(JsonValue)) {
                return "{} | null";
            }
            if (type == typeof(string)) {
                return "string";
            }
            if (type == typeof(bool)) {
                return "boolean";
            }
            if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                || type == typeof(float) || type == typeof(double)) {
                return "number";
            }
            if (mapper.IsArray) {
                var elementMapper = mapper.GetElementMapper();
                var isOpt = false;
                var elementTypeName = GetFieldType(elementMapper, context, ref isOpt);
                return $"{elementTypeName}[]";
            }
            var isDictionary = type.GetInterfaces().Contains(typeof(IDictionary));
            if (isDictionary) {
                var valueMapper = mapper.GetElementMapper();
                var isOpt = false;
                var valueTypeName = GetFieldType(valueMapper, context, ref isOpt);
                return $"{{ [key: string]: {valueTypeName} }}";
            }
            context.imports.Add(type);
            if (context.generator.IsUnionType(type))
                return $"{type.Name}_Union";
            return type.Name;
        }
        
        private void EmitPackageHeaders(StringBuilder sb) {
            foreach (var pair in generator.packages) {
                Package package     = pair.Value;
                string  packageName = pair.Key;
                sb.Clear();
                sb.AppendLine($"// {Generator.Note}");
                var     max         = package.imports.MaxLength(import => import.Value.package == packageName ? 0 : import.Key.Name.Length);

                foreach (var importPair in package.imports) {
                    var import = importPair.Value;
                    if (import.package == packageName)
                        continue;
                    var typeName = import.type.Name;
                    var indent = Generator.Indent(max, typeName);
                    sb.AppendLine($"import {{ {typeName} }}{indent} from \"./{import.package}\"");
                    if (generator.IsUnionType(import.type)) {
                        sb.AppendLine($"import {{ {typeName}_Union }}{indent} from \"./{import.package}\"");
                    }
                }
                package.header = sb.ToString();
            }
        }
    }
}