// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class JsonWriter : IDisposable
    {
        /// <summary>Caches type mata data per thread and provide stats to the cache utilization</summary>
        public readonly     TypeCache   typeCache;
        public              Bytes       bytes;
        /// <summary>Can be used for custom mappers append a number while creating the JSON payload</summary>
        public              ValueFormat format;
        /// <summary>Can be used for custom mappers to create a temporary "string"
        /// without creating a string on the heap.</summary>
        public              Bytes       strBuf;

        internal            Bytes       @null = new Bytes("null");
        internal            Bytes       discriminator = new Bytes("\"$type\":\"");

        public          ref Bytes Output => ref bytes;

        internal            int         level;
        public              int         Level => level;
        public              int         maxDepth;
        

        public JsonWriter(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
            maxDepth = 100;
        }
        
        public void Dispose() {
            typeCache.Dispose();
            @null.Dispose();
            discriminator.Dispose();
            format.Dispose();
            strBuf.Dispose();
            bytes.Dispose();
        }
        /*
        public void Write(object obj) { 
            StubType stubType = typeCache.GetType(obj.GetType());
            Var valueVar = new Var();
            valueVar.Obj = obj;
            WriteStart(stubType, ref valueVar);
        } */
        
        public void Write<T>(T value) {
            var stubType = (TypeMapper<T>)typeCache.GetType(typeof(T));
            
            WriteStart(stubType, value);
        }
        
        /*
        public void Write<T>(ref Var valueVar) { 
            StubType stubType = typeCache.GetType(typeof(T));
            WriteStart(stubType, ref valueVar);
        } */
        
        private void WriteStart<T>(TypeMapper<T> mapper, T value) {
            bytes.  InitBytes(128);
            strBuf. InitBytes(128);
            format. InitTokenFormat();
            bytes.Clear();
            level = 0;
            if (EqualityComparer<T>.Default.Equals(value, default))
                WriteUtils.AppendNull(this);
            else
                mapper.Write(this, value);
            
            if (level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {level}");
        }
    }
}
