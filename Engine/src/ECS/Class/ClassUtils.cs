﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

internal static class ClassType<T> where T : class
{
    // ReSharper disable once StaticMemberInGenericType
    internal static readonly    int     ClassIndex  = ClassUtils.NewClassIndex(typeof(T));
}

public static class ClassUtils
{
    internal const              int                                 MissingAttribute            = 0;
    
    private  static             int                                 _nextClassIndex             = 1;
    private  static readonly    Dictionary<Type, Bytes>             ClassComponentBytes         = new Dictionary<Type, Bytes>();
    private  static readonly    Dictionary<Type, string>            ClassComponentKeys          = new Dictionary<Type, string>();
    public   static             IReadOnlyDictionary<Type, string>   RegisteredClassComponentKeys => ClassComponentKeys;

    internal static int NewClassIndex(Type type) {
        foreach (var attr in type.CustomAttributes) {
            if (attr.AttributeType == typeof(ClassComponentAttribute)) {
                var arg = attr.ConstructorArguments;
                var key = (string) arg[0].Value;
                ClassComponentKeys.Add(type, key);
                ClassComponentBytes.Add(type, new Bytes(key));
                return _nextClassIndex++;
            }
        }
        return MissingAttribute;
    }
    
    internal static string GetClassKey(Type type) {
        return ClassComponentKeys[type];
    }
    
    internal static Bytes GetClassKeyBytes(Type type) {
        return ClassComponentBytes[type];
    }
}