﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Friflo.Json.Burst;

namespace Friflo.Json.Flow.Schema.Validation
{
    public class JsonValidator : IDisposable
    {
        private             Bytes           jsonBytes = new Bytes(128);
        private             JsonParser      parser;
        private             string          errorMsg;
        private  readonly   List<bool[]>    foundFieldsCache = new List<bool[]>();
        private  readonly   StringBuilder   sb = new StringBuilder();
        private  readonly   Regex           dateTime;
        
        // RFC 3339 + milliseconds
        private  static readonly Regex      DateTime =  new Regex(@"\b^[1-9]\d{3}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}.\d{3}Z$\b", RegexOptions.Compiled);
        
        public              bool            qualifiedTypeErrors;
        
        public JsonValidator (bool qualifiedTypeErrors = false) {
            this.qualifiedTypeErrors = qualifiedTypeErrors;
            dateTime = DateTime;
        }
        
        public void Dispose() {
            parser.Dispose();
            jsonBytes.Dispose();
            foundFieldsCache.Clear();
            sb.Clear();
        }
        
        private void Init(string json) {
            errorMsg = null;
            jsonBytes.Clear();
            jsonBytes.AppendString(json);
            parser.InitParser(jsonBytes);
        }
        
        private bool Return(ValidationType type, bool success, out string error) {
            if (!success) {
                error = errorMsg;
                return false;
            }
            var ev = parser.NextEvent();
            if (ev == JsonEvent.EOF) {
                error = null;
                return true;
            }
            return RootError(type, "Expected EOF after reading JSON", out error);
        }

        public bool ValidateObject (string json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateObject(type, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, $"ValidateObject() expect object. was: {ev}", out error);
        }
        
        public bool ValidateObjectMap (string json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ObjectStart) {
                bool success = ValidateElement(type, false, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, $"ValidateObjectMap() expect object. was: {ev}", out error);
        }

        public bool ValidateArray (string json, ValidationType type, out string error) {
            Init(json);
            var ev = parser.NextEvent();
            if (ev == JsonEvent.ArrayStart) {
                bool success = ValidateElement(type, true, 0);
                return Return(type, success, out error);    
            }
            return RootError(type, $"ValidateArray() expect array. was: {ev}", out error);
        }
        
        private bool ValidateObject (ValidationType type, int depth)
        {
            if (type.typeId == TypeId.Union) {
                var ev      = parser.NextEvent();
                var unionType = type.unionType;
                if (ev != JsonEvent.ValueString) {
                    return Error(type, $"Expect discriminator as first member. Expect: '{unionType.discriminatorStr}', was: {ev}");
                }
                if (!parser.key.IsEqual(ref unionType.discriminator)) {
                    return Error(type, $"Unexpected discriminator name. was: {parser.key}, expect: {unionType.discriminatorStr}");
                }
                if (!ValidationUnion.FindUnion(unionType, ref parser.value, out var newType)) {
                    return Error(type, $"Unknown discriminant: '{parser.value}'");
                }
                type = newType;
            }
            var foundFields = GetFoundFields(type, foundFieldsCache, depth);

            while (true) {
                var             ev = parser.NextEvent();
                ValidationField field;
                string          msg;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(type, msg);
                        if (ValidateString (ref parser.value, field.type, out msg))
                            continue;
                        return Error(type, msg);
                        
                    case JsonEvent.ValueNumber:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(type, msg);
                        if (ValidateNumber(ref parser, field.type, out msg))
                            continue;
                        return Error(type, msg);
                        
                    case JsonEvent.ValueBool:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(type, msg);
                        if (field.typeId == TypeId.Boolean)
                            continue;
                        return Error(type, $"Found boolean but expect: {field.typeId}");
                    
                    case JsonEvent.ValueNull:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(type, msg);
                        if (!field.required)
                            continue;
                        return Error(type, $"Found null for required field.");
                    
                    case JsonEvent.ArrayStart:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(type, msg);
                        if (field.isArray) {
                            if (ValidateElement (field.type, true, depth))
                                continue;
                            return false;
                        }
                        return Error(type, $"Found array but expect: {field.typeId}");
                    
                    case JsonEvent.ObjectStart:
                        if (!ValidationType.FindField(type, ref parser.key, out field, out msg, foundFields))
                            return Error(type, msg);
                        if (field.typeId == TypeId.Complex) {
                            if (field.isDictionary) {
                                if (ValidateElement (field.type, false, depth))
                                    continue;
                                return false;
                            }
                            if (ValidateElement (field.type, true, depth))
                                continue;
                            return false;
                        }
                        return Error(type, $"Found object but expect: {field.typeId}");
                    
                    case JsonEvent.ObjectEnd:
                        if (type.HasMissingFields(foundFields, out var missingFields)) {
                            var missing = string.Join(", ", missingFields);
                            return Error(type, $"Missing required fields: [{missing}]");
                        }
                        return true;
                    
                    case JsonEvent.ArrayEnd:
                        return Error(type, $"Found array end ']' in object: {ev}");
                    
                    case JsonEvent.Error:
                        return Error(type, parser.error.GetMessageBody());

                    default:
                        return Error(type, $"Unexpected JSON event in object: {ev}");
                }
            }
        }
        
        private bool ValidateElement (ValidationType type, bool isArray, int depth) {
            while (true) {
                var     ev = parser.NextEvent();
                string  msg;
                switch (ev) {
                    case JsonEvent.ValueString:
                        if (ValidateString(ref parser.value, type, out msg))
                            continue;
                        return Error(type, msg);
                        
                    case JsonEvent.ValueNumber:
                        if (ValidateNumber(ref parser, type, out msg))
                            continue;
                        return Error(type, msg);
                        
                    case JsonEvent.ValueBool:
                        if (type.typeId == TypeId.Boolean)
                            continue;
                        return Error(type, $"Found boolean but expect: {type.typeId}");
                    
                    case JsonEvent.ValueNull:
                        return Error(type, $"Found null for required value.");
                    
                    case JsonEvent.ArrayStart:
                        return Error(type, $"Found array as array item. expect: {type.typeId}");
                    
                    case JsonEvent.ObjectStart:
                        if (type.typeId == TypeId.Complex || type.typeId == TypeId.Union) {
                            // in case of a dictionary the key is not relevant
                            if (ValidateObject(type, depth + 1))
                                continue;
                            return false;
                        }
                        return Error(type, $"Found object but expect: {type.typeId}");
                    
                    case JsonEvent.ObjectEnd:
                        if (!isArray)
                            return true;
                        return Error(type, $"Found object end '}}' in array: {ev}");
                    
                    case JsonEvent.ArrayEnd:
                        if (isArray)
                            return true;
                        return Error(type, $"Found array end ']' in object: {ev}");
                    
                    case JsonEvent.Error:
                        return Error(type, parser.error.GetMessageBody());

                    default:
                        return Error(type, $"Unexpected JSON event: {ev}");
                }
            }
        }
        
        private bool RootError (ValidationType type, string msg, out string error) {
            if (parser.Event == JsonEvent.Error) {
                Error(type, parser.error.GetMessageBody());
            } else {
                Error(type, msg);
            }
            error = errorMsg;
            return false;
        }
        
        private bool Error (ValidationType type, string msg) {
            if (errorMsg != null)
                throw new InvalidOperationException($"error already set. Error: {errorMsg}");
            sb.Clear();
            sb.Append(msg);
            sb.Append(" - type: ");
            if (qualifiedTypeErrors) {
                sb.Append(type);
            } else {
                sb.Append(type.name);
            }
            sb.Append(", path: ");
            sb.Append(parser.GetPath());
            sb.Append(", pos: ");
            sb.Append(parser.Position);
            errorMsg = sb.ToString();
            return false;
        }
        
        // --- helper methods
        private bool ValidateString (ref Bytes value, ValidationType type, out string msg) {
            switch (type.typeId) {
                case TypeId.String:
                    msg = null;
                    return true;
                case TypeId.BigInteger:
                    msg = null; // todo
                    return true;
                case TypeId.DateTime:
                    var str = value.ToString();
                    if (dateTime.IsMatch(str)) {
                        msg = null;
                        return true;
                    }
                    msg = $"Invalid DateTime: '{str}'";
                    return false;

                case TypeId.Enum:
                    return ValidationType.FindEnum(type, ref value, out msg);
                default:
                    msg = $"Found string but expect: {type.typeId}";
                    return false;
            }
        }
        
        private static bool ValidateNumber (ref JsonParser parser, ValidationType type, out string msg) {
            switch (type.typeId) {
                case TypeId.Uint8:
                case TypeId.Int16:
                case TypeId.Int32:
                case TypeId.Int64:
                    if (!parser.isFloat) {
                        msg = null;
                        return true;
                    }
                    msg = $"Found floating point number but expect {type.typeId}";
                    return false;
                case TypeId.Float:
                case TypeId.Double:
                    msg = null;
                    return true;
                default:
                    msg = $"Found number but expect: {type.typeId}";
                    return false;
            }
        }
        
        private static bool[] GetFoundFields(ValidationType type, List<bool[]> foundFieldsCache, int depth) {
            while (foundFieldsCache.Count <= depth) {
                foundFieldsCache.Add(null);
            }
            int requiredCount = type.requiredFieldsCount;
            bool[] foundFields = foundFieldsCache[depth];
            if (foundFields == null || foundFields.Length < requiredCount) {
                foundFields = foundFieldsCache[depth] = new bool[requiredCount];
            }
            for (int n= 0; n < requiredCount; n++) {
                foundFields[n] = false;
            }
            return foundFields;
        }
    }
}