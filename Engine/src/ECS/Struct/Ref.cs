﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

/// <summary>
/// Enables access to a struct component by reference using its property <see cref="Value"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public struct Ref<T> where T : struct
{
    /// <summary>
    /// Returns a mutable struct component value by reference.<br/>
    /// <see cref="Value"/> modifications are instantaneously available via <see cref="GameEntity.GetComponentValue{T}"/>  
    /// </summary>
    public          ref T       Value => ref components[pos];
    
    private             T[]     components;
    internal            int     pos;
    
    internal void Set(T[] components) {
        this.components = components;
    }
    
    internal void Set(T[] components, ref T[] copy, int count) {
        if (copy == null) {
            this.components = components;
            return;
        }
        if (copy.Length < count) {
            copy = new T[components.Length];
        }
        Array.Copy(components, copy, count);
        this.components = copy;
    }

    public  override    string  ToString() => Value.ToString();
}