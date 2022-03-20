﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Schema.Lang;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Schema
{
    /// <summary>
    /// The sync models define a protocol. The generated Typescript are useful for client applications.
    /// JSON Schema files are not generated as the host (server) validate protocol messages by code.  
    /// </summary>
    public static class FilterGen
    {
        private static readonly Type[] FilterTypes        = { typeof(FilterOperation) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var options     = new NativeTypeOptions(FilterTypes);
            var generator   = TypescriptGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Typescript/Filter");
        }
        
        /// C# -> JSON Schema
        [Test]
        public static void CS_JSON () {
            var options     = new NativeTypeOptions(FilterTypes);
            var generator   = JsonSchemaGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/JSON/Filter");
        }
        
        /// C# -> C#
        // [Test]
        public static void CS_CS () {
            var options     = new NativeTypeOptions(FilterTypes) {
                replacements = new[]{new Replace("Friflo.Json.")}
            };
            var generator = CSharpGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/C#/Filter");
        }
        
        /// C# -> Kotlin
        [Test]
        public static void CS_Kotlin () {
            var options     = new NativeTypeOptions(FilterTypes);
            var generator = KotlinGenerator.Generate(options);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets~/Schema/Kotlin/src/main/kotlin/Filter");
        }
    }
}