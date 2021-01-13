// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Prop;

namespace Friflo.Json.Managed
{
    public class JsonWriter : IDisposable
    {
        public readonly     PropType.Cache typeCache;
        public              Bytes       bytes;
        public              ValueFormat format;
        private             Bytes       strBuf;

        public              Bytes       @null = new Bytes("null");
        public              Bytes       discriminator = new Bytes("\"$type\":\"");

        public          ref Bytes Output => ref bytes;

        public JsonWriter(TypeStore typeStore) {
            typeCache = new PropType.Cache(typeStore);
        }
        
        public void Dispose() {
            @null.Dispose();
            discriminator.Dispose();
            format.Dispose();
            strBuf.Dispose();
            bytes.Dispose();
        }

        public void Write(Object obj) {
            bytes.InitBytes(128);
            strBuf.InitBytes(128);
            format.InitTokenFormat();
            NativeType objType = typeCache.GetType(obj.GetType());
            WriteJson(obj, objType);
        }

        public void WriteKey(PropField field) {
            bytes.AppendChar('\"');
            field.AppendName(ref bytes);
            bytes.AppendString("\":");
        }

        public void WriteString(String str) {
            bytes.AppendChar('\"');
            strBuf.Clear();
            strBuf.FromString(str);
            JsonSerializer.AppendEscString(ref bytes, ref strBuf);
            bytes.AppendChar('\"');
        }
        
        /// <summary>
        /// Is called for every JSON object found during iteration 
        /// </summary>
        public void WriteJson(Object obj, NativeType nativeType) {
            if (nativeType.jsonCodec != null)
                nativeType.jsonCodec.Write(this, obj, nativeType);
            else
                throw new NotSupportedException("found no resolver for JSON object: " + nativeType.type.FullName);
        }
    }
}