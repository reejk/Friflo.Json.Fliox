﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Mapper.Map.Val;

namespace Friflo.Json.Flow.Schema.JSON
{
    public class JsonType
    {
        public  string                          discriminator;
        public  List<FieldType>                 oneOf;
        //
        // public  SchemaType?                  type; // todo use this
        public  string                          type;
        public  Dictionary<string, FieldType>   properties;
        public  List<string>                    required;
        public  bool                            additionalProperties;
        //
        [Fri.Property(Name = "$ref")]
        public  JsonType                        reference;
        //
        [Fri.Property(Name = "enum")]
        public  List<string>                    enums;
    }
    
    public class FieldType
    {
        public  JsonValue       type; // SchemaType or SchemaType[]
        
        [Fri.Property(Name = "enum")]
        public  List<string>    discriminant;
        
        public  List<FieldType> items;
        
        [Fri.Property(Name = "$ref")]
        public  FieldType       reference;

        public  FieldType       additionalProperties;
    }
    
    public enum SchemaType {
        [Fri.EnumValue(Name = "null")]
        Null,
        [Fri.EnumValue(Name = "object")]
        Object,
        [Fri.EnumValue(Name = "string")]
        String,
        [Fri.EnumValue(Name = "boolean")]
        Boolean,
        [Fri.EnumValue(Name = "number")]
        Number,
        [Fri.EnumValue(Name = "integer")]
        Integer,
        [Fri.EnumValue(Name = "array")]
        Array
    }
}