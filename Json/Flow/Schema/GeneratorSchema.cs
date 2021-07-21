﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Schema
{
    public class GeneratorSchema
    {
        private readonly TypeStore typeStore;
        
        public GeneratorSchema (TypeStore typeStore, ICollection<Type> rootTypes) {
            this.typeStore = typeStore;
            foreach (var type in rootTypes) {
                typeStore.GetTypeMapper(type);
            }
        }
        
        public Generator Typescript() {
            var generator = new Generator(typeStore);
            var typescript = new Typescript(generator);
            typescript.GenerateSchema();
            return generator;
        }
            
        public Generator JsonSchema (bool separateEntities) {
            var generator = new Generator(typeStore);
            var jsonSchema = new JsonSchema(generator, separateEntities);
            jsonSchema.GenerateSchema();
            return generator;
        }
    }
}