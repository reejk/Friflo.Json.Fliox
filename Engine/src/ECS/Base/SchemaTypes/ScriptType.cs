﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

internal delegate object CloneScript(object instance);

/// <summary>
/// Provide meta data for a <see cref="Script"/> class. 
/// </summary>
public abstract class ScriptType : SchemaType
{
    /// <summary> Ihe index in <see cref="EntitySchema"/>.<see cref="EntitySchema.Scripts"/>. </summary>
    public   readonly   int             ScriptIndex;    //  4
    /// <summary> Return true if <see cref="Script"/>'s of this type can be copied. </summary>
    public   readonly   bool            IsBlittable;    //  4
    private  readonly   CloneScript     cloneScript;    //  8
    
    internal abstract   Script          CreateScript();
    
    protected ScriptType(string scriptKey, int scriptIndex, Type type)
        : base (scriptKey, type, SchemaTypeKind.Script)
    {
        ScriptIndex = scriptIndex;
        IsBlittable   = GetBlittableType(type) == BlittableType.Blittable;
        if (IsBlittable) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;
            var methodInfo      = type.GetMethod("MemberwiseClone", flags);
            // Create a delegate representing an 'open instance method'.
            // An instance must be passed when the delegate is invoked.
            // See: https://learn.microsoft.com/en-us/dotnet/api/system.delegate.createdelegate
            var cloneDelegate   = Delegate.CreateDelegate(typeof(CloneScript), null, methodInfo!);
            cloneScript         = (CloneScript)cloneDelegate;
        }
    }
    
    internal Script CloneScript(Script original)
    {
        var clone = cloneScript(original);
        return (Script)clone;
    }
}

internal sealed class ScriptType<T> : ScriptType 
    where T : Script, new()
{
    public  override    string          ToString() => $"Script: [*{typeof(T).Name}]";
    
    // Check initialization by directly calling unit test method: Test_SchemaType.Test_SchemaType_Script_Index()
    internal static readonly   int      Index = SchemaTypeUtils.GetScriptIndex(typeof(T));
    
    internal ScriptType(string scriptComponentKey, int scriptIndex)
        : base(scriptComponentKey, scriptIndex, typeof(T))
    {
    }
    
    internal override Script CreateScript() {
        return new T();
    }
}
