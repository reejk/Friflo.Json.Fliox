﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Friflo.Json.Flow.Mapper.Map;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Schema.Utils.Mapper
{
    public class NativeTypeSystem : ITypeSystem
    {
        private  readonly   ICollection<ITyp> types;
        
        private  readonly   NativeType  boolean;
        private  readonly   NativeType  @string;
        private  readonly   NativeType  uint8;
        private  readonly   NativeType  int16;
        private  readonly   NativeType  int32;
        private  readonly   NativeType  int64;
        private  readonly   NativeType  flt32;
        private  readonly   NativeType  flt64;
        private  readonly   NativeType  bigInteger;
        private  readonly   NativeType  dateTime;
        private  readonly   NativeType  jsonValue;

        public              ICollection<ITyp>               Types => types;
        /// Contains only non nullable Type's
        private  readonly   Dictionary<Type, NativeType>    nativeMap;
        
        public              ITyp    Boolean    => boolean;
        public              ITyp    String     => @string;
        public              ITyp    Unit8      => uint8;
        public              ITyp    Int16      => int16;
        public              ITyp    Int32      => int32;
        public              ITyp    Int64      => int64;
        public              ITyp    Float      => flt32;
        public              ITyp    Double     => flt64;
        public              ITyp    BigInteger => bigInteger;
        public              ITyp    DateTime   => dateTime;
        public              ITyp    JsonValue  => jsonValue;
        
        public NativeTypeSystem (IReadOnlyDictionary<Type, TypeMapper> typeMappers) {
            nativeMap   = new Dictionary<Type, NativeType>(typeMappers.Count);
            var map     = new Dictionary<ITyp, TypeMapper>(typeMappers.Count);
            foreach (var pair in typeMappers) {
                TypeMapper  mapper  = pair.Value;
                var underMapper     = mapper.GetUnderlyingMapper();
                IsNullableMapper(typeMappers, underMapper, out var nonNullableMapper);
                Type type           = nonNullableMapper.type;
                if (nativeMap.ContainsKey(type))
                    continue;
                var  iTyp           = new NativeType(underMapper);
                nativeMap.Add(type, iTyp);
                map.      Add(iTyp, underMapper);
            }
            this.types = map.Keys;
            nativeMap.TryGetValue(typeof(bool),         out boolean);
            nativeMap.TryGetValue(typeof(string),       out @string);
            nativeMap.TryGetValue(typeof(byte),         out uint8);
            nativeMap.TryGetValue(typeof(short),        out int16);
            nativeMap.TryGetValue(typeof(int),          out int32);
            nativeMap.TryGetValue(typeof(long),         out int64);
            nativeMap.TryGetValue(typeof(float),        out flt32);
            nativeMap.TryGetValue(typeof(double),       out flt64);
            nativeMap.TryGetValue(typeof(BigInteger),   out bigInteger);
            nativeMap.TryGetValue(typeof(DateTime),     out dateTime);
            nativeMap.TryGetValue(typeof(JsonValue),    out jsonValue);
            
            foreach (var pair in nativeMap) {
                NativeType  type        = pair.Value;
                Type        baseType    = type.native.BaseType;
                TypeMapper  mapper;
                // When searching for polymorph base class there may be are classes in this hierarchy. E.g. BinaryBoolOp. 
                // If these classes may have a protected constructor they need to be skipped. These classes have no TypeMapper. 
                while (!typeMappers.TryGetValue(baseType, out  mapper)) {
                    baseType = baseType.BaseType;
                    if (baseType == null)
                        break;
                }
                if (mapper != null) {
                    type.baseType = nativeMap[mapper.type];
                }
            }
            foreach (var pair in nativeMap) {
                NativeType  type    = pair.Value;
                TypeMapper  mapper  = type.mapper;
                var elementMapper = mapper.GetElementMapper();
                if (elementMapper != null) {
                    elementMapper = elementMapper.GetUnderlyingMapper();
                    type.ElementType    = nativeMap[elementMapper.type];
                }
                var  propFields = mapper.propFields;
                if (propFields != null) {
                    type.fields = new List<Field>(propFields.fields.Length);
                    foreach (var propField in propFields.fields) {
                        var fieldMapper = propField.fieldType.GetUnderlyingMapper();
                        var isNullable = IsNullableMapper(typeMappers, fieldMapper, out var nonNullableMapper);
                        var field = new Field {
                            jsonName    = propField.jsonName,
                            required    = propField.required || !isNullable,
                            fieldType   = nativeMap[nonNullableMapper.type]
                        };
                        type.Fields.Add(field);
                    }
                }
                
                var instanceFactory = mapper.instanceFactory;
                if (instanceFactory != null) {
                    var polyTypes = instanceFactory.polyTypes;
                    type.unionType = new UnionType {
                        discriminator = instanceFactory.discriminator,
                        polyTypes = new List<ITyp>(polyTypes.Length)
                    };
                    foreach (var polyType in polyTypes) {
                        ITyp element = nativeMap[polyType.type];
                        type.unionType.polyTypes.Add(element);
                    }
                }
            }
        }
   
        private static bool IsNullableMapper(IReadOnlyDictionary<Type, TypeMapper> typeMappers, TypeMapper mapper, out TypeMapper nonNullableMapper) {
            var isNullable = mapper.isNullable;
            if (isNullable && mapper.nullableUnderlyingType != null) {
                var type = mapper.nullableUnderlyingType;
                nonNullableMapper = typeMappers[type];
                return true;
            }
            nonNullableMapper = mapper;
            return isNullable;
        }
        
        public ICollection<ITyp> GetTypes(ICollection<Type> nativeTypes) {
            if (nativeTypes == null)
                return null;
            var list = new List<ITyp> (nativeTypes.Count);
            foreach (var nativeType in nativeTypes) {
                var type = nativeMap[nativeType];
                list.Add(type);
            }
            return list;
        }
    }
}