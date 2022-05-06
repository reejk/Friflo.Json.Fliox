﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Json.Burst;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Validation
{
    internal enum TypeId
    {
        None,
        // --- object types
        Class,
        Union,
        // --- number types
        Uint8,
        Int16,
        Int32,
        Int64,
        Float,
        Double,
        // --- boolean type
        Boolean,   
        // --- string types        
        String,
        BigInteger,
        DateTime,
        Guid,
        Enum,
        //
        JsonValue 
    }
    
    /// <summary>
    /// Similar to <see cref="Definition.TypeDef"/> but operates on byte arrays instead of strings to gain
    /// performance.
    /// </summary>
    internal sealed class ValidationTypeDef  {
        internal  readonly  string              name;
        internal  readonly  string              @namespace;
        
        // --- intern / private
        internal  readonly  TypeDef             typeDef;
        private   readonly  string              qualifiedName;
        internal  readonly  TypeId              typeId;
        private   readonly  ValidationType[]    fields;
        internal  readonly  int                 requiredFieldsCount;
        private   readonly  ValidationType[]    requiredFields;
        internal  readonly  ValidationUnion     unionType;
        private   readonly  Utf8String[]        enumValues;
        internal  readonly  ValidationType      validationType;
        
        internal            IEnumerable<ValidationType> Fields      => fields;
        public    override  string                      ToString()  => qualifiedName;
        
        internal ValidationTypeDef (TypeId typeId, string typeName, bool isNullable, TypeDef typeDef) {
            this.typeId         = typeId;
            this.typeDef        = typeDef;
            this.name           = typeName;
            this.@namespace     = typeDef.Namespace;
            this.qualifiedName  = $"{@namespace}.{name}";
            validationType      = new ValidationType(this, isNullable, false, false);
        }
        
        private ValidationTypeDef (TypeDef typeDef, UnionType union)               : this (TypeId.Union, typeDef.Name, true, typeDef) {
            unionType       = new ValidationUnion(union);
        }
        
        private ValidationTypeDef (TypeDef typeDef, IReadOnlyList<FieldDef> fieldDefs) : this (TypeId.Class, typeDef.Name, true, typeDef) {
            int requiredCount = 0;
            foreach (var field in fieldDefs) {
                if (field.required)
                    requiredCount++;
            }
            requiredFieldsCount = requiredCount;
            requiredFields      = new ValidationType[requiredCount];
            fields              = new ValidationType[fieldDefs.Count];
            int n = 0;
            int requiredPos = 0;
            foreach (var field in fieldDefs) {
                var reqPos      = field.required ? requiredPos++ : -1;
                var fieldType   = new ValidationType(field, reqPos);
                fields[n++] = fieldType;
                if (reqPos >= 0)
                    requiredFields[reqPos] = fieldType;
            }
        }
        
        private ValidationTypeDef (TypeDef typeDef, IReadOnlyList<EnumValue> typeEnums) : this (TypeId.Enum, typeDef.Name, false, typeDef) {
            enumValues = new Utf8String[typeEnums.Count];
            int n = 0;
            foreach (var enumValue in typeEnums) {
                enumValues[n++] = enumValue.nameUtf8;
            }
        }

        internal static ValidationTypeDef Create (TypeDef typeDef) {
            var union = typeDef.UnionType;
            if (union != null) {
                return new ValidationTypeDef(typeDef, union);
            }
            if (typeDef.IsClass) {
                return new ValidationTypeDef(typeDef, typeDef.Fields);
            }
            if (typeDef.IsEnum) {
                return new ValidationTypeDef(typeDef, typeDef.EnumValues);
            }
            return null;
        }
        
        internal static string GetName (ValidationTypeDef typeDef, bool qualified) {
            var typeId = typeDef.typeId; 
            if (typeId == TypeId.Class || typeId == TypeId.Union|| typeId == TypeId.Enum) {
                if (qualified) {
                    return typeDef.qualifiedName;
                }
                return typeDef.name;
            }
            return typeDef.name;
        }

        internal void SetFields(Dictionary<TypeDef, ValidationTypeDef> typeMap) {
            if (fields != null) {
                foreach (var field in fields) {
                    var fieldType   = typeMap[field.type];
                    field.typeDef   = fieldType;
                    field.typeId    = fieldType.typeId;
                    if (fieldType.typeId == TypeId.None) throw new InvalidOperationException("TypeId.None");
                }
            }
        }
        
        internal static bool FindEnum (ValidationTypeDef typeDef, ref Bytes value, TypeValidator validator, ValidationTypeDef parent) {
            var enumValues = typeDef.enumValues;
            for (int n = 0; n < enumValues.Length; n++) {
                if (enumValues[n].IsEqual(ref value)) {
                    return true;
                }
            }
            return validator.ErrorType("Invalid enum value.", value.AsString(), true, typeDef.name, typeDef.@namespace, parent);
        }
        
        internal static bool FindField (ValidationTypeDef typeDef, TypeValidator validator, out ValidationType fieldType, bool[] foundFields) {
            ref var parser = ref validator.parser;
            foreach (var field in typeDef.fields) {
                if (!field.name.IsEqual(ref parser.key))
                    continue;
                fieldType   = field;
                var reqPos  = fieldType.requiredPos;
                if (reqPos >= 0) {
                    foundFields[reqPos] = true;
                }
                var ev = parser.Event; 
                if (ev != JsonEvent.ArrayStart && ev != JsonEvent.ValueNull && fieldType.isArray) {
                    var value       = GetValue(ref parser, out bool isString);
                    validator.ErrorType("Incorrect type.", value, isString, fieldType.typeName, fieldType.typeDef.@namespace, typeDef);
                    return false;
                }
                return true;
            }
            validator.ErrorValue("Unknown property:", parser.key.AsString(), true, typeDef);
            fieldType = null;
            return false;
        }
        
        private static string GetValue(ref Utf8JsonParser parser, out bool isString) {
            isString = false;
            switch (parser.Event) {
                case JsonEvent.ValueString:     isString = true;
                                                return parser.value.AsString();
                case JsonEvent.ObjectStart:     return "object";
                case JsonEvent.ValueNumber:     return parser.value.AsString();
                case JsonEvent.ValueBool:       return parser.boolValue ? "true" : "false";
                case JsonEvent.ValueNull:       return "null";
                default:
                    return parser.Event.ToString();
            }
        }

        internal bool HasMissingFields(bool[] foundFields, StringBuilder sb) {
            var foundCount = 0;
            for (int n = 0; n < requiredFieldsCount; n++) {
                if (foundFields[n])
                    foundCount++;
            }
            var missingCount = requiredFieldsCount - foundCount;
            if (missingCount == 0) {
                return false;
            }
            bool first = true;
            sb.Clear();
            sb.Append('[');
            for (int n = 0; n < requiredFieldsCount; n++) {
                if (!foundFields[n]) {
                    if (first) {
                        first = false;
                    } else {
                        sb.Append(", ");
                    }
                    var fieldName = requiredFields[n].fieldName;
                    sb.Append(fieldName);
                }
            }
            sb.Append(']');
            return true;
        }
    }
}