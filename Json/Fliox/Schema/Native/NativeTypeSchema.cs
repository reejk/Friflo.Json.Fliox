﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Mapper.Map.Obj.Reflect;
using Friflo.Json.Fliox.Mapper.Map.Utils;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Schema.Native
{
    public sealed class NativeTypeSchema : TypeSchema, IDisposable
    {
        public   override   ICollection<TypeDef>    Types           { get; }
        public   override   StandardTypes           StandardTypes   { get; }
        public   override   TypeDef                 RootType        { get; }
        
        public              TypeDef                 TypeAsTypeDef(Type type) => nativeTypes[type];

        /// <summary>Contains only non <see cref="Nullable"/> Type's</summary>
        private  readonly   Dictionary<Type, NativeTypeDef> nativeTypes;
        

        public NativeTypeSchema (Type rootType) : this (new [] { rootType }, rootType){
        }

        public NativeTypeSchema (ICollection<Type> typeList, Type rootType = null) {
          using (var typeStore = new TypeStore()) {
            typeStore.AddMappers(typeList);
            var typeMappers = typeStore.GetTypeMappers();
            TypeMapper rootTypeMapper = rootType == null ? null : typeMappers[rootType];

            // Collect all types into containers to simplify further processing
            nativeTypes     = new Dictionary<Type, NativeTypeDef>(typeMappers.Count);
            var types       = new List<TypeDef>                  (typeMappers.Count);
            foreach (var pair in typeMappers) {
                TypeMapper  mapper  = pair.Value;
                AddType(types, mapper, typeStore);
            }
            /* typeMappers = typeStore.GetTypeMappers();
            foreach (var pair in typeMappers) {
                TypeMapper  mapper  = pair.Value;
                if (mapper.type == typeof(Guid?)) {
                    int x = 1;
                }
                if (mapper.type == typeof(Guid)) {
                    int x = 1;
                }
                AddType(types, mapper, typeStore);
            } */
            // in case any Nullable<> was found - typeStore contain now also their non-nullable counterparts.
            typeMappers = typeStore.GetTypeMappers();
            
            var standardTypes = new NativeStandardTypes(nativeTypes);
            StandardTypes   = standardTypes;

            // set the base type (base class or parent class) for all types. 
            foreach (var pair in nativeTypes) {
                NativeTypeDef   typeDef     = pair.Value;
                Type            baseType    = typeDef.native.BaseType;
                TypeMapper      mapper;
                // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
                // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
                while (!typeMappers.TryGetValue(baseType, out  mapper)) {
                    baseType = baseType.BaseType;
                    if (baseType == null)
                        break;
                }
                if (mapper != null) {
                    typeDef.baseType = nativeTypes[mapper.type];
                }
            }
            foreach (var pair in nativeTypes) {
                NativeTypeDef   typeDef = pair.Value;
                TypeMapper      mapper  = typeDef.mapper;
                
                // set the fields for classes or structs
                var  propFields         = mapper.propFields;
                if (propFields != null) {
                    typeDef.keyField        = propFields.GetField("id") != null ? "id" : null;
                    typeDef.fields = new List<FieldDef>(propFields.fields.Length);
                    foreach (var propField in propFields.fields) {
                        var fieldMapper     = propField.fieldType.GetUnderlyingMapper();
                        var isNullable      = IsNullableMapper(fieldMapper, out var nonNullableType) ||
                                              fieldMapper.type == typeof(JsonValue);
                        var isArray         = fieldMapper.IsArray;
                        var isDictionary    = fieldMapper.IsDictionary;
                        NativeTypeDef type;
                        bool isNullableElement = false;
                        Type    relationType;
                        if (isArray || isDictionary) {
                            var elementMapper       = fieldMapper.GetElementMapper();
                            var underlyingMapper    = elementMapper.GetUnderlyingMapper();
                            if(underlyingMapper.isValueType && underlyingMapper.isNullable) {
                                IsNullableMapper(underlyingMapper, out var nonNullableElementType);
                                type = nativeTypes[nonNullableElementType];
                                isNullableElement = true;
                            } else {
                                type = nativeTypes[underlyingMapper.type];
                            }
                            relationType    = elementMapper.RelationType();
                        } else {
                            type            = nativeTypes[nonNullableType];
                            relationType    = propField.fieldType.RelationType();
                        }
                        string relation     = ContainerFromType(rootTypeMapper, relationType);
                        relation            = relation ?? propField.GetRelationAttributeType();
                        var required        = propField.required || !isNullable;
                        var isKey           = propField.isKey;
                        if (isKey) {
                            typeDef.keyField = propField.jsonName;
                        }
                        bool isAutoIncrement = FieldQuery.IsAutoIncrement(propField.Member.CustomAttributes);
                        
                        var fieldDef = new FieldDef (propField.jsonName, required, isKey, isAutoIncrement, type, isArray, isDictionary, isNullableElement, typeDef, relation);
                        typeDef.fields.Add(fieldDef);
                    }
                }
                var commands = CommandUtils.GetCommandTypes(typeDef.native);
                if (commands != null) {
                    var commandDefs = new List<CommandDef>(commands.Length);
                    typeDef.commands = commandDefs;
                    foreach (var command in commands) {
                        var valueType   = nativeTypes[command.valueType];
                        var resultType  = nativeTypes[command.resultType];
                        var commandDef  = new CommandDef(command.name, valueType, resultType);
                        commandDefs.Add(commandDef);
                    }
                }
                if (typeDef.Discriminant != null) {
                    var baseType = typeDef.baseType;
                    while (baseType != null) {
                        var unionType = baseType.unionType;
                        if (unionType != null) {
                            typeDef.discriminator = unionType.discriminator;
                            break;
                        }
                        baseType = baseType.baseType;
                    }
                    if (typeDef.discriminator == null)
                        throw new InvalidOperationException($"found no discriminator in base classes. type: {typeDef}");
                }
                // set the unionType if a class is a discriminated union
                var instanceFactory = mapper.instanceFactory;
                if (instanceFactory != null) {
                    typeDef.isAbstract = true;
                    // expect polyTypes if not abstract
                    if (!instanceFactory.isAbstract) {
                        var polyTypes   = instanceFactory.polyTypes;
                        var unionTypes  = new List<UnionItem>(polyTypes.Length);
                        foreach (var polyType in polyTypes) {
                            TypeDef element = nativeTypes[polyType.type];
                            var item = new UnionItem (element, polyType.name);
                            unionTypes.Add(item);
                        }
                        typeDef.unionType  = new UnionType (instanceFactory.discriminator, unionTypes);
                    }
                }
            }
            MarkDerivedFields(types);
            if (rootType != null) {
                var rootTypeDef = TypeAsTypeDef(rootType);
                if (rootTypeDef == null)
                    throw new InvalidOperationException($"rootType not found: {rootType}");
                if (!rootTypeDef.IsClass)
                    throw new InvalidOperationException($"rootType must be a class: {rootType}");
                SetKeyField(rootTypeDef);
                SetRelationTypes(rootTypeDef, types);
                RootType = rootTypeDef;
            }
            Types           = types;
          }
        }
        
        public void Dispose() { }
        
        private void AddType(List<TypeDef> types, TypeMapper typeMapper, TypeStore typeStore) {
            var mapper  = typeMapper.GetUnderlyingMapper();
            if (IsNullableMapper(mapper, out var nonNullableType)) {
                mapper = typeStore.GetTypeMapper(nonNullableType);
            }
            if (nativeTypes.ContainsKey(nonNullableType))
                return;
            NativeTypeDef typeDef;
            if (NativeStandardTypes.Types.TryGetValue(nonNullableType, out string name)) {
                typeDef = new NativeTypeDef(mapper, name, "Standard");
            } else {
                typeDef = new NativeTypeDef(mapper, nonNullableType.Name, nonNullableType.Namespace);
            }
            nativeTypes.Add(nonNullableType, typeDef);
            types.      Add(typeDef);
            
            var baseType = mapper.BaseType;
            if (baseType != null) {
                var baseMapper = typeStore.GetTypeMapper(baseType);
                AddType(types, baseMapper, typeStore);
            }
            /* var instanceFactory = mapper.instanceFactory;
            if (instanceFactory != null) {
                // expect polyTypes if not abstract
                if (!instanceFactory.isAbstract) {
                    var polyTypes   = instanceFactory.polyTypes;
                    foreach (var polyType in polyTypes) {
                        var polyTypeDef = typeStore.GetTypeMapper(polyType.type);
                        AddType(types, polyTypeDef, typeStore);
                    }
                }
            } */
        }
        
        private static bool IsNullableMapper(TypeMapper mapper, out Type nonNullableType) {
            var isNullable = mapper.isNullable;
            if (isNullable && mapper.nullableUnderlyingType != null) {
                nonNullableType = mapper.nullableUnderlyingType;
                return true;
            }
            nonNullableType = mapper.type;
            return isNullable;
        }
        
        private static string ContainerFromType(TypeMapper rootTypeMapper, Type relationType) {
            if (rootTypeMapper == null || relationType == null)
                return null;
            foreach (var field in rootTypeMapper.propFields.fields) {
                var elementMapper = field.fieldType.GetElementMapper();
                if (elementMapper.type == relationType) {
                    return field.name;
                } 
            }
            return null;
        }
        
        public ICollection<TypeDef> TypesAsTypeDefs(ICollection<Type> types) {
            if (types == null)
                return null;
            var list = new List<TypeDef> (types.Count);
            foreach (var nativeType in types) {
                var type = nativeTypes[nativeType];
                list.Add(type);
            }
            return list;
        }
    }
}