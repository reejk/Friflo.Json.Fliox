﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Schema.Definition
{
    /// <summary>
    /// <see cref="TypeSchema"/> is an abstraction of an entire type system / schema used as input for code generators
    /// and JSON payload validation like <b>JSON Schema</b>.
    /// <br></br>
    /// The abstraction enables:
    /// <list type="bullet">
    ///   <item>
    ///     Simplify implementation of code generators as its API is tailored towards retrieving type information by
    ///     simple getters using <see cref="TypeDef"/>, <see cref="FieldDef"/>, <see cref="UnionType"/> and <see cref="MessageDef"/>.
    ///   </item>
    ///   <item>
    ///     Write code generators independent from the specific used <see cref="TypeSchema"/> like
    ///     <see cref="JSON.JsonTypeSchema"/> or <see cref="Native.NativeTypeSchema"/>. 
    ///   </item>
    ///   <item>
    ///     Enable implementation of <see cref="Validation.TypeValidator"/> being independent from a specific
    ///     <see cref="TypeSchema"/> like <see cref="JSON.JsonTypeSchema"/> or <see cref="Native.NativeTypeSchema"/>.
    ///   </item>
    ///   <item>
    ///     Resolving all type references by <see cref="TypeDef"/>'s defined in a type system / schema in advance
    ///     to simplify type access and avoiding type lookups. E.g. references like <see cref="TypeDef.BaseType"/> or
    ///     <see cref="FieldDef.type"/>. 
    ///   </item>
    /// </list>  
    /// <br></br>
    /// Note: This file does and must not have any dependency to <see cref="System.Type"/>. <br/>
    /// Note: <see cref="TypeSchema"/> instances are immutable.
    /// </summary>
    public abstract class TypeSchema
    {
        /// <summary>Set of all types defined in the type system / schema.</summary>
        public abstract     IReadOnlyList<TypeDef>  Types           { get; }
        /// <summary>Set of all well known / standard types used in the type system / schema like integers,
        /// floating point numbers, strings, booleans and timestamps</summary>
        public abstract     StandardTypes           StandardTypes   { get; }
        
        public abstract     TypeDef                 RootType        { get; }

        private             Dictionary<TypeDefKey, TypeDef> typeDefMap;

        public              TypeDef                 FindTypeDef(string @namespace, string name) {
            if (typeDefMap == null) {
                typeDefMap = new Dictionary<TypeDefKey, TypeDef>(Types.Count);
                var types = Types;
                foreach (var typeDef in types) {
                    bool isNamedType = typeDef.IsClass || typeDef.IsStruct || typeDef.IsEnum;  
                    if (!isNamedType)
                        continue;
                    var key = new TypeDefKey(typeDef.Namespace, typeDef.Name);
                    typeDefMap.Add(key, typeDef);
                }
            }
            var key2 = new TypeDefKey(@namespace, name);
            return typeDefMap[key2];
        }

        public Dictionary<string, TypeDef> GetEntityTypes() {
            var map = new Dictionary<string, TypeDef>(RootType.Fields.Count);
            foreach (var field in RootType.Fields) {
                map.Add(field.name, field.type);
            }
            return map;
        }
        
        /// <summary>
        /// Must to be called after collecting all <see cref="Types"/> and their <see cref="TypeDef.Fields"/>.
        /// </summary>
        protected static void MarkDerivedFields (ICollection<TypeDef> types) {
            foreach (var type in types) {
                var fields = type.Fields;
                if (fields == null)
                    continue;
                foreach (var typeField in fields) {
                    typeField.MarkDerivedField();
                }
            }
        }
        
        protected static  void SetRelationTypes (TypeDef rootTypeDef, ICollection<TypeDef> types) {
            foreach (var type in types) {
                var fields = type.Fields;
                if (fields == null)
                    continue;
                foreach (var typeField in fields) {
                    var relation = typeField.relation;
                    if (relation == null)
                        continue;
                    typeField.relationType = rootTypeDef.FindField(relation).type;
                }
            }
        }
        
        protected static void  SetKeyField(TypeDef rootType) {
            if (!rootType.IsSchema)
                return;
            var rootFields = rootType.Fields;
            foreach (var rootField in rootFields) {
                var jsonType = rootField.type;
                if (jsonType.keyField != null)
                    continue;
                bool foundId = false;
                foreach (var field in jsonType.Fields) {
                    if (field.name == "id") {
                        foundId = true;
                        break;
                    }
                }
                if (!foundId)
                    throw new InvalidOperationException($"missing entity identifier at: {jsonType}");
                jsonType.keyField = "id";
            }
        }
        
        /// <summary>
        /// Create a list from the given  <paramref name="types"/> in the order:
        /// <list>
        ///   <item>root type - schema</item>
        ///   <item>field types of root type - entity types</item>
        ///   <item>remaining types</item>
        /// </list>
        /// </summary>
        protected static List<TypeDef> OrderTypes(TypeDef rootType, List<TypeDef> types) {
            if (rootType == null)
                return types;
            var typeMap = new Dictionary<string, TypeDef>(types.Count);
            typeMap.Add(rootType.fullName, rootType);
            foreach (var entityField in rootType.Fields) {
                var type = entityField.type;
                typeMap.TryAdd(type.fullName, type);
            }
            foreach (var type in types) {
                typeMap.TryAdd(type.fullName, type);
            }
            return new List<TypeDef>(typeMap.Values);
        }
    }
    
    internal sealed class TypeDefKey
    {
        private readonly    string  @namespace;
        private readonly    string  name;
        
        internal TypeDefKey(string @namespace, string name) {
            this.@namespace = @namespace;
            this.name       = name;
        }

        public override string ToString() => $"{@namespace}.{name}";

        public override int GetHashCode() {
            return @namespace.GetHashCode() ^ name.GetHashCode();
        }

        public override bool Equals(object obj) {
            // ReSharper disable once PossibleNullReferenceException
            var other = (TypeDefKey)obj; // boxes - doesnt matter
            return @namespace == other.@namespace && name == other.name;
        }
    }
}