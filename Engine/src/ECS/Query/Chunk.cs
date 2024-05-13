﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// A <see cref="Chunk{T}"/> is container of <b>struct</b> components of Type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// <see cref="Chunk{T}"/>'s are typically returned a <see cref="ArchetypeQuery{T1}"/>.<see cref="ArchetypeQuery{T1}.Chunks"/> enumerator.<br/>
/// <br/>
/// Its items can be accessed or changed with <see cref="this[int]"/> or <see cref="Span"/>.<br/>
/// The <see cref="Chunk{T}"/> implementation also support <b>vectorization</b>
/// of <a href="https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/vectorization-guidelines.md">Vector types</a><br/>
/// by <see cref="AsSpan128{TTo}"/>, <see cref="AsSpan256{TTo}"/> and <see cref="AsSpan512{TTo}"/>.
/// <br/>
/// <br/> <i>See vectorization example</i> at <see cref="AsSpan256{TTo}"/>.
/// </remarks>
/// <typeparam name="T"><see cref="IComponent"/> type of a struct component.</typeparam>
[DebuggerTypeProxy(typeof(ChunkDebugView<>))]
public readonly struct Chunk<T>
    where T : struct, IComponent
{
    /// <summary> Return the components in a <see cref="Chunk{T}"/> as a <see cref="Span"/>. </summary>
    public              Span<T>     Span            => new(values, start, Length);
    
    private  readonly   T[]         values;     //  8
    
    /// <summary> Return the number of components in a <see cref="Chunk{T}"/>. </summary>
    public   readonly   int         Length;     //  4
    
    // ReSharper disable once NotAccessedField.Local
    private  readonly   int         start;      //  4
    
    /// <summary>
    /// Return the components as a <see cref="Span{TTo}"/> of type <typeparamref name="TTo"/> - which can be assigned to Vector256{TTo}'s.<br/>
    /// The returned <see cref="Span{TTo}"/> contains padding elements on its tail to enable safe conversion to a Vector256{TTo}.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#query-vectorization---simd">Example.</a>.
    /// </summary>
    /// <remarks>
    /// By adding padding elements the returned <see cref="Span{TTo}"/> can be converted to Vector256's <br/>
    /// without the need of an additional <b>for</b> loop to process the elements at the tail of the <see cref="Span{T}"/>.<br/>
    /// <br/>
    /// <i>Vectorization example:</i><br/>
    /// <code>
    ///     // e.g. using: struct ByteComponent : IComponent { public byte value; }
    ///     var add = Vector256.Create&lt;byte>(1);                // create byte[32] vector - all values = 1
    ///     foreach (var (component, _) in query.Chunks)
    ///     {    
    ///         var bytes   = component.AsSpan256&lt;byte>();      // bytes.Length - multiple of 32
    ///         var step    = component.StepSpan256;            // step = 32
    ///         for (int n = 0; n &lt; bytes.Length; n += step) {
    ///             var slice   = bytes.Slice(n, step);
    ///             var value   = Vector256.Create&lt;byte>(slice);
    ///             var result  = Vector256.Add(value, add);    // execute 32 add instructions at once
    ///             result.CopyTo(slice);
    ///         }
    ///     }
    /// </code>
    /// </remarks>
    public              Span<TTo>  AsSpan256<TTo>() where TTo : struct
                        => MemoryMarshal.Cast<T, TTo>(new Span<T>(values, 0, (Length + ComponentType<T>.PadCount256) & 0x7fff_ffe0));
    
    /// <summary>
    /// Return the components as a <see cref="Span{TTo}"/> of type <typeparamref name="TTo"/>.<br/>
    /// The returned <see cref="Span{TTo}"/> contains padding elements on its tail to enable assignment to Vector128{TTo}.<br/>
    /// See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#query-vectorization---simd">Example.</a>.
    /// </summary>
    public              Span<TTo>  AsSpan128<TTo>() where TTo : struct
                        => MemoryMarshal.Cast<T, TTo>(new Span<T>(values, 0, (Length + ComponentType<T>.PadCount128) & 0x7fff_fff0));
    
    /// <summary>
    /// Return the components as a <see cref="Span{TTo}"/> of type <typeparamref name="TTo"/>.<br/>
    /// The returned <see cref="Span{TTo}"/> contains padding elements on its tail to enable assignment to Vector512.<br/>
    ///  See <a href="https://github.com/friflo/Friflo.Json.Fliox/blob/main/Engine/README.md#query-vectorization---simd">Example.</a>.
    /// </summary>
    public              Span<TTo>  AsSpan512<TTo>() where TTo : struct
                        => MemoryMarshal.Cast<T, TTo>(new Span<T>(values, 0, (Length + ComponentType<T>.PadCount512) & 0x7fff_ffc0));
    
    /// <summary>
    /// The step value in a for loop when converting a <see cref="AsSpan128{TTo}"/> value to a Vector128{T}.
    /// <br/><br/> See example at <see cref="AsSpan256{TTo}"/>.
    /// </summary>
    [Browse(Never)]
    public              int         StepSpan128 => 16 / ComponentType<T>.ByteSize;
    
    /// <summary>
    /// The step value in a for loop when converting a <see cref="AsSpan256{TTo}"/> value to a Vector256{T}.
    /// <br/><br/> See example at <see cref="AsSpan256{TTo}"/>.
    /// </summary>
    [Browse(Never)]
    public              int         StepSpan256 => 32 / ComponentType<T>.ByteSize;
    
    // ReSharper disable once InvalidXmlDocComment
    /// <summary>
    /// The step value in a for loop when converting a <see cref="AsSpan512{TTo}"/> value to a <c>Vector512{T}</c>
    /// <br/><br/> See example at <see cref="AsSpan256{TTo}"/>.
    /// </summary>
    [Browse(Never)]
    public              int         StepSpan512 => 64 / ComponentType<T>.ByteSize;

    public override     string      ToString()  => $"{typeof(T).Name}[{Length}]";


    internal Chunk(T[] values, T[] copy, int length) {
        Length      = length;
        if (copy == null) {
            this.values = values;
        } else {
            Array.Copy(values, copy, length);
            this.values = copy;
        }
    }
    
    internal Chunk(Chunk<T> chunk, int start, int length) {
        Length      = length;
        this.start  = start;
        values      = chunk.values;
    }
    
    /// <summary> Return the component at the passed <paramref name="index"/> as a reference. </summary>
    public ref T this[int index] {
        get {
            if (index < Length) {
                return ref values[index];
            }
            throw new IndexOutOfRangeException();
        }
    }
}

internal class ChunkDebugView<T>
    where T : struct, IComponent
{
    [Browse(RootHidden)]
    public              T[]         Components => chunk.Span.ToArray();

    [Browse(Never)]
    private readonly    Chunk<T>    chunk;
        
    internal ChunkDebugView(Chunk<T> chunk)
    {
        this.chunk = chunk;
    }
}