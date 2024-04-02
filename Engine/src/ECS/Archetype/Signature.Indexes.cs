﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Friflo.Engine.ECS;

/// <summary>
/// Note: The order of struct type indices matters.<br/>
/// The struct is used when dealing with generic types like: T1, T2, T3, ...   
/// </summary>
internal readonly struct SignatureIndexes
{
    internal readonly   int     length; // 4    - values: [1, 5] ensured by assertion
    
    internal readonly   byte    T1;     // 1
    internal readonly   byte    T2;     // 1
    internal readonly   byte    T3;     // 1
    internal readonly   byte    T4;     // 1
    internal readonly   byte    T5;     // 1
    private  readonly   byte    T6;     // 1
    private  readonly   byte    T7;     // 1
    private  readonly   byte    T8;     // 1
    private  readonly   byte    T9;     // 1
    private  readonly   byte    T10;    // 1
    
    public   SignatureIndexesEnumerator GetEnumerator() => new (this);
    
    public override     string          ToString()      => GetString("SignatureIndexes: ");
    
    internal SignatureIndexes (
        int length,
        int T1  = 0,
        int T2  = 0,
        int T3  = 0,
        int T4  = 0,
        int T5  = 0,
        int T6  = 0,
        int T7  = 0,
        int T8  = 0,
        int T9  = 0,
        int T10 = 0
    ) {
        AssertLength(length);
        this.length = length;
        this.T1     = (byte)T1;
        this.T2     = (byte)T2;
        this.T3     = (byte)T3;
        this.T4     = (byte)T4;
        this.T5     = (byte)T5;
        this.T6     = (byte)T6;
        this.T7     = (byte)T7;
        this.T8     = (byte)T8;
        this.T9     = (byte)T9;
        this.T10    = (byte)T10;
    }
    
    [ExcludeFromCodeCoverage]
    [Conditional("DEBUG")]
    private static void AssertLength(int length) {
        if (length is < 1 or > 10) {
            throw new IndexOutOfRangeException();
        }
    }
    
    internal int GetStructIndex(int index)
    {
        switch (index) {
            case 0:     return T1;
            case 1:     return T2;
            case 2:     return T3;
            case 3:     return T4;
            case 4:     return T5;
            case 5:     return T6;
            case 6:     return T7;
            case 7:     return T8;
            case 8:     return T9;
            case 9:     return T10;
        //  default:    throw new IndexOutOfRangeException(); // unreachable - already ensured by constructor
        }
        return -1;  // unreachable - all valid cases are covered
    }
    
    internal readonly string GetString (string prefix) {
        var sb = new StringBuilder();
        if (prefix != null) {
            sb.Append(prefix);
        }
        sb.Append('[');
        var components = EntityStoreBase.Static.EntitySchema.components;
        for (int n = 0; n < length; n++)
        {
            var structIndex = GetStructIndex(n);
            sb.Append(components[structIndex].Name);
            sb.Append(", "); 
        }
        sb.Length -= 2;
        sb.Append(']');
        return sb.ToString();
    }
}


internal struct SignatureIndexesEnumerator
{
    private readonly    SignatureIndexes   indexes;
    private             int             index;
    
    internal SignatureIndexesEnumerator(in SignatureIndexes indexes)
    {
        this.indexes    = indexes;
        index           = -1;
    }
    
    public int Current => indexes.GetStructIndex(index);

    // --- IEnumerator
    public bool MoveNext() {
        if (index < indexes.length - 1) {
            index++;
            return true;
        }
        return false;
    }
}


