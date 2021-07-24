﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Utils.Mapper
{
public class NativeType : ITyp
    {
        internal readonly   Type                native;
        internal readonly   TypeMapper          mapper;
        internal            ITyp                baseType;
        internal            List<Field>         fields;
        private             ITyp                elementType;
        internal            UnionType           unionType;
        
        public   override   string              Name            => native.Name;
        public   override   string              Namespace       => native.Namespace;
        public   override   ITyp                BaseType        => baseType;
        public   override   bool                IsEnum          => native.IsEnum;
        public   override   bool                IsComplex       => mapper.IsComplex;
        public   override   List<Field>         Fields          => fields;
        public   override   string              Discriminant    => mapper.Discriminant;
        public   override   TypeSemantic        TypeSemantic    => mapper.GetTypeSemantic();
        public   override   bool                IsNullable      => mapper.isNullable;
        public   override   bool                IsArray         => mapper.IsArray;
        public   override   UnionType           UnionType       => unionType;


        public   override   ITyp                ElementType {   get          => elementType;
                                                                internal set => elementType = value;  }

        public   override   bool                IsDictionary    => mapper.type.GetInterfaces().Contains(typeof(IDictionary));
        public   override   string              ToString()      => mapper.type.ToString();
        
        public   override   ICollection<string> GetEnumValues() => mapper.GetEnumValues();
        
        public   override   bool                IsDerivedField(Field field) {
            var parent = BaseType;
            while (parent != null) {
                if (parent.Fields.Find(f => f.jsonName == field.jsonName) != null)
                    return true;
                parent = parent.BaseType;
            }
            return false;    
        }
           
        public NativeType (TypeMapper mapper) {
            this.native     = mapper.type;
            this.mapper     = mapper;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                throw new NullReferenceException();
            var other = (NativeType)obj;
            return native == other.native;
        }

        public override int GetHashCode() {
            return (native != null ? native.GetHashCode() : 0);
        }
    }
}