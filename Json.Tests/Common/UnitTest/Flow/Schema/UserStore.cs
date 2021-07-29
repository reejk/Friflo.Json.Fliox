﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Flow.Graph;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Schema;
using Friflo.Json.Flow.Schema.JSON;
using Friflo.Json.Flow.UserAuth;
using Friflo.Json.Tests.Common.UnitTest.Flow.Schema.Misc;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest.Flow.Schema
{
    public static class UserStoreSchema
    {
        private static readonly Type[] UserStoreTypes   = { typeof(Role), typeof(UserCredential), typeof(UserPermission) };

        // -------------------------------------- input: C# --------------------------------------
        
        /// C# -> Typescript
        [Test]
        public static void CS_Typescript () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = TypescriptGenerator.Generate(typeStore, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/Typescript/UserStore");
        }
        
        /// C# -> JSON Schema
        [Test, Order(1)]
        public static void CS_JSON () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonSchemaGenerator.Generate(typeStore, UserStoreTypes, null, UserStoreTypes);
            generator.WriteFiles(JsonSchemaFolder);
        }
        
        /// C# -> C#
        [Test]
        public static void CS_CS () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = CSharpGenerator.Generate(typeStore, UserStoreTypes, new[]{"Friflo.Json."});
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/CSharp/UserStore");
        }
        
        /// C# -> JTD
        [Test]
        public static void CS_JTD () {
            var typeStore = EntityStore.AddTypeMatchers(new TypeStore());
            var generator = JsonTypeDefinition.Generate(typeStore, UserStoreTypes, "UserStore", null, UserStoreTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema/JTD/", false);
        }
        
        // ---------------------------------- input: JSON Schema ----------------------------------
        
        static readonly string JsonSchemaFolder = CommonUtils.GetBasePath() + "assets/Schema/JSON/UserStore";
        
        
        /// JSON Schema -> Typescript
        [Test]
        public static void JSON_Typescript () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var generator   = TypescriptGenerator.Generate(schema, jsonTypes);
            generator.WriteFiles(CommonUtils.GetBasePath() + "assets/Schema-Loop/Typescript/UserStore");
        }
        
        /// JSON Schema -> JSON Schema
        [Test, Order(2)]
        public static void JSON_JSON () {
            var schemas     = JsonTypeSchema.ReadSchemas(JsonSchemaFolder);
            var schema      = new JsonTypeSchema(schemas);
            var jsonTypes   = SchemaTest.JsonTypesFromTypes (UserStoreTypes, "Friflo.Json.Flow.UserAuth.");
            var typeDefs    = schema.TypesAsTypeDefs(jsonTypes);
            var generator   = JsonSchemaGenerator.Generate(schema, jsonTypes, typeDefs);
            
            var loopFolder = CommonUtils.GetBasePath() + "assets/Schema-Loop/JSON/UserStore";
            generator.WriteFiles(loopFolder, false);
            SchemaTest.AssertFoldersAreEqual(JsonSchemaFolder, loopFolder);
        }
    }
}