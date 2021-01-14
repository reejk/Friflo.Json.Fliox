// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Managed.Prop;

namespace Friflo.Json.Managed
{
    // JsonReader
    public class JsonReader : IDisposable
    {
        public          JsonParser      parser;
        public readonly TypeCache       typeCache;

        public readonly Bytes           discriminator = new Bytes("$type");

        public          JsonError       Error => parser.error;
        public          SkipInfo        SkipInfo => parser.skipInfo;

        public JsonReader(TypeStore typeStore) {
            typeCache = new TypeCache(typeStore);
            parser = new JsonParser {error = {throwException = false}};
        }

        public void Dispose() {
            discriminator.Dispose();
            parser.Dispose();
        }
        
        public T ReadValue <T>(Bytes bytes) where T : struct {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadStart(bytes.buffer, start, len, typeof(T));
            parser.NextEvent(); // EOF
            if (ret == null) {
                if (Error.ErrSet)
                    throw new InvalidOperationException(parser.error.msg.ToString()); 
                throw new InvalidOperationException("cannot assign null to value type: " + typeof(T).FullName);
            }
            return (T) ret;
        }
        
        public T Read<T>(Bytes bytes) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadStart(bytes.buffer, start, len, typeof(T));
            parser.NextEvent(); // EOF
            if (typeof(T).IsPrimitive && ret == null && parser.error.ErrSet)
                throw new InvalidOperationException(parser.error.msg.ToString());
            return (T) ret;
        }
        
        public Object Read(Bytes bytes, Type type) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadStart(bytes.buffer, start, len, type);
            parser.NextEvent(); // EOF
            return ret;
        }

        public Object Read(ByteList buffer, int offset, int len, Type type) {
            var ret = ReadStart(buffer, offset, len, type);
            parser.NextEvent(); // EOF
            return ret;
        }

        private Object ReadStart(ByteList bytes, int offset, int len, Type type) {
            parser.InitParser(bytes, offset, len);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        NativeType propType = typeCache.GetType(type); // lookup required
                        return ReadJson(null, propType, 0);
                    case JsonEvent.ArrayStart:
                        NativeType collection = typeCache.GetType(type); // lookup required 
                        return ReadJson(null, collection, 0);
                    case JsonEvent.ValueString:
                        NativeType valueType = typeCache.GetType(type);
                        return ReadJson(null, valueType, 0);
                    case JsonEvent.ValueNumber:
                        valueType = typeCache.GetType(type);
                        return ReadJson(null, valueType, 0);
                    case JsonEvent.ValueBool:
                        valueType = typeCache.GetType(type);
                        return ReadJson(null, valueType, 0);
                    case JsonEvent.ValueNull:
                        valueType = typeCache.GetType(type);
                        return ReadJson(null, valueType, 0);
                    case JsonEvent.Error:
                        return null;
                    default:
                        return ErrorNull("unexpected state in Read() : ", ev);
                }
            }
        }

        public Object ReadTo(Bytes bytes, Object obj) {
            int start = bytes.Start;
            int len = bytes.Len;
            var ret = ReadTo(bytes.buffer, start, len, obj);
            parser.NextEvent();
            return ret;
        }

        public Object ReadTo(ByteList bytes, int offset, int len, Object obj) {
            parser.InitParser(bytes, offset, len);

            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ObjectStart:
                        NativeType propType = typeCache.GetType(obj.GetType()); // lookup required
                        return ReadJson(obj, propType, 0);
                    case JsonEvent.ArrayStart:
                        NativeType collection = typeCache.GetType(obj.GetType()); // lookup required
                        return ReadJson(obj, collection, 0);
                    case JsonEvent.Error:
                        return null;
                    default:
                        return ErrorNull("ReadTo() can only used on an JSON object or array", ev);
                }
            }
        }
        
        public Object ErrorNull(string msg, string value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value);
            return null;
        }

        public Object ErrorNull(string msg, JsonEvent ev) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + ev.ToString());
            return null;
        }

        public Object ErrorNull(string msg, ref Bytes value) {
            // TODO use message / value pattern as in JsonParser to avoid allocations by string interpolation
            parser.Error("JsonReader", msg + value.ToStr32());
            return null;
        }
        
        /** Method only exist to find places, where token (numbers) are parsed. E.g. in or double */
        public Object ValueParseError() {
            return null; // ErrorNull(parser.parseCx.GetError().ToString());
        }

        public static readonly int minLen = 8;

        public static int Inc(int len) {
            return len < 5 ? minLen : 2 * len;
        }

        //
        public object NumberFromValue(SimpleType.Id? id, out bool success) {
            if (id == null) {
                success = false;
                return null;
            }

            switch (id) {
                case SimpleType.Id.Long:
                    return parser.ValueAsLong(out success);
                case SimpleType.Id.Integer:
                    return parser.ValueAsInt(out success);
                case SimpleType.Id.Short:
                    return parser.ValueAsShort(out success);
                case SimpleType.Id.Byte:
                    return parser.ValueAsByte(out success);
                case SimpleType.Id.Double:
                    return parser.ValueAsDouble(out success);
                case SimpleType.Id.Float:
                    return parser.ValueAsFloat(out success);
                default:
                    success = false;
                    return ErrorNull("Cant convert number to: ", id.ToString());
            }
        }

        public object BoolFromValue(SimpleType.Id? id, out bool success) {
            if (id == SimpleType.Id.Bool)
                return parser.ValueAsBool(out success);
            success = false;
            return ErrorNull("Cant convert number to: ", id.ToString());
        }
        
        public Object ArrayUnexpected (JsonReader reader, JsonEvent ev) {
            return reader.ErrorNull("unexpected state in array: ", ev);
        }
        
        /// <summary>
        /// Is called for every JSON object & array found during JSON iteration 
        /// </summary>
        public Object ReadJson(Object obj, NativeType nativeType, int index) {
            return nativeType.jsonCodec.Read(this, obj, nativeType);
        }
    }
}
