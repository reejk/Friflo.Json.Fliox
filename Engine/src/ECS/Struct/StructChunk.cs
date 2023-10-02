﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Fliox.Engine.ECS;

internal readonly struct StructChunk<T>
    where T : struct
{
    internal readonly T[]       components;
    
    public override string ToString() => components == null ? "" : "used";
    
    internal StructChunk (int count) {
        components  = new T[count];
    }
}

