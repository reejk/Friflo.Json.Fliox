﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Mapper.Map;

namespace Friflo.Json.Flow.Schema.Definition
{
    public abstract class TypeDef {
        public              string              Name         { get; set; }
        /// <summary>Namespace of a type.</summary>
        public              string              Namespace    { get; set; }
        /// <summary>The path of the file a type is generated.
        /// <br></br>
        /// In Typescript or JSON Schema a file can contain multiple types.
        /// These types have the same file <see cref="Path"/> and the same <see cref="Namespace"/>.
        /// <br></br>
        /// In Java each type require its own file. The <see cref="Namespace"/> is the Java package name which can be
        /// used by multiple types. But each type has its individual file <see cref="Path"/>.
        /// </summary>
        public              string              Path         { get; internal set; }
        public  abstract    TypeDef             BaseType     { get; }
        public  abstract    bool                IsEnum       { get; }
        public  abstract    bool                IsComplex    { get; }
        public  abstract    List<Field>         Fields       { get; }
        public  abstract    string              Discriminant { get; }
        public  abstract    bool                IsArray      { get; }
        public              TypeDef             ElementType  { get; set; }
        public  abstract    bool                IsDictionary { get; }
        public  abstract    UnionType           UnionType    { get; }
        public  abstract    ICollection<string> EnumValues   { get; }
        /// currently not used
        public  abstract    TypeSemantic        TypeSemantic { get; }

        public  abstract    bool                IsDerivedField(Field field);
    }
    
    public class Field {
        public              string          name;
        public              bool            required;
        public              TypeDef         type;

        public   override   string          ToString() => name;
    }

    public class UnionType {
        public              string          discriminator;
        public              List<TypeDef>   types;
        
        public   override   string          ToString() => discriminator;
    }
}