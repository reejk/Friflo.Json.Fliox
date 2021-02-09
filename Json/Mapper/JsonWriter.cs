// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial class JsonWriter : IDisposable
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
        internal            Bytes       discriminator;

        public          ref Bytes Output => ref bytes;

        internal            int         level;
        public              int         Level => level;
        public              int         maxDepth;
        

        public JsonWriter(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
            discriminator = new Bytes($"\"{typeStore.config.discriminator}\":\"");
            maxDepth = 100;
            // useIL = typeStore.config.useIL;
        }
        
        public void Dispose() {
            typeCache.Dispose();
            @null.Dispose();
            discriminator.Dispose();
            format.Dispose();
            strBuf.Dispose();
            bytes.Dispose();
            DisposeMirrorStack();
        }

        private void InitJsonWriter() {
            bytes.  InitBytes(128);
            strBuf. InitBytes(128);
            format. InitTokenFormat();
            bytes.Clear();
            level = 0;
            InitMirrorStack();
        }

        public void WriteObject(object value) { 
            TypeMapper stubType = typeCache.GetTypeMapper(value.GetType());
            WriteStart(stubType, value);
        }
        
        public void Write<T>(T value) {
            var m = typeCache.GetTypeMapper(typeof(T));
            var mapper = (TypeMapper<T>) m;
            
            WriteStart(mapper, value);
        }
        
        private void WriteStart(TypeMapper mapper, object value) {
            InitJsonWriter();

            if (value == null) {
                WriteUtils.AppendNull(this);
            } else {
                try {
                    mapper.WriteObject(this, value);
                }
                finally { ClearMirrorStack(); }
            }

            if (level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {level}");
        }
        
        private void WriteStart<T>(TypeMapper<T> mapper, T value) {
            InitJsonWriter();
            try {
                if (mapper.IsNull(ref value))
                    WriteUtils.AppendNull(this);
                else
                    mapper.Write(this, value);
            }
            finally { ClearMirrorStack(); }
            

            if (level != 0)
                throw new InvalidOperationException($"Unexpected level after JsonWriter.Write(). Expect 0, Found: {level}");
        }
    }
}
