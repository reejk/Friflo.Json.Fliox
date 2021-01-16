﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Types;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Codecs
{
    public class ObjectArrayCodec : IJsonCodec
    {
        public static readonly ObjectArrayCodec Interface = new ObjectArrayCodec();
        
        public StubType CreateStubType(Type type) {
            if (type. IsArray) {
                Type elementType = type.GetElementType();
                int rank = type.GetArrayRank();
                if (rank > 1)
                    return null; // todo implement multi dimensional array support
                if (Reflect.IsAssignableFrom(typeof(Object), elementType)) {
                    ConstructorInfo constructor = null; // For arrays Arrays.CreateInstance(componentType, length) is used
                    // ReSharper disable once ExpressionIsAlwaysNull
                    return new CollectionType(type, elementType, this, type.GetArrayRank(), null, constructor);
                }
            }
            return null;
        }
        
        public void Write (JsonWriter writer, object obj, StubType stubType) {
            CollectionType collectionType = (CollectionType) stubType;
            Array arr = (Array) obj;
            writer.bytes.AppendChar('[');
            StubType elementType = collectionType.ElementType;
            for (int n = 0; n < arr.Length; n++) {
                if (n > 0) writer.bytes.AppendChar(',');
                object item = arr.GetValue(n);
                if (item == null)
                    writer.bytes.AppendBytes(ref writer.@null);
                else
                    elementType.codec.Write(writer, item, elementType);
            }
            writer.bytes.AppendChar(']');
        }

        public bool Read(JsonReader reader, ref Slot slot, StubType stubType) {
            if (!ArrayUtils.IsArrayStart(reader, stubType))
                return false;
            
            ref var parser = ref reader.parser;
            var collection = (CollectionType) stubType;
            int startLen;
            int len;
            Array array;
            if (slot.Obj == null) {
                startLen = 0;
                len = JsonReader.minLen;
                array = Arrays.CreateInstance(collection.ElementType.type, len);
            }
            else {
                array = (Array) slot.Obj;
                startLen = len = array.Length;
            }

            StubType elementType = collection.ElementType;
            int index = 0;
            Slot elemSlot = new Slot();
            while (true) {
                JsonEvent ev = parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        // array of string, bool, int, long, float, double, short, byte are handled via primitive array codecs
                        return reader.ErrorIncompatible("array element", elementType, ref parser);
                    case JsonEvent.ValueNull:
                        if (index >= len)
                            array = Arrays.CopyOfType(collection.ElementType.type, array, len = JsonReader.Inc(len));
                        if (!elementType.isNullable)
                            return reader.ErrorIncompatible("array element", elementType, ref parser);
                        array.SetValue(null, index++);
                        break;
                    case JsonEvent.ArrayStart:
                        StubType subElementArray = collection.ElementType;
                        if (index < startLen) {
                            elemSlot.Obj = array.GetValue(index);
                            if(!subElementArray.codec.Read(reader, ref elemSlot, subElementArray))
                                return false;
                            array.SetValue(elemSlot.Obj, index);
                        }
                        else {
                            elemSlot.Clear();
                            if (!subElementArray.codec.Read(reader, ref elemSlot, subElementArray))
                                return false;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.ElementType.type, array, len = JsonReader.Inc(len));
                            array.SetValue(elemSlot.Obj, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ObjectStart:
                        if (index < startLen) {
                            elemSlot.Obj = array.GetValue(index);
                            if (!elementType.codec.Read(reader, ref elemSlot, elementType))
                                return false;
                            array.SetValue(elemSlot.Obj, index);
                        }
                        else {
                            elemSlot.Clear();
                            if (!elementType.codec.Read(reader, ref elemSlot, elementType))
                                return false;
                            if (index >= len)
                                array = Arrays.CopyOfType(collection.ElementType.type, array, len = JsonReader.Inc(len));
                            array.SetValue(elemSlot.Obj, index);
                        }

                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (index != len)
                            array = Arrays.CopyOfType(collection.ElementType.type, array, index);
                        slot.Obj = array;
                        return true;
                    case JsonEvent.Error:
                        return false;
                    default:
                        return reader.ErrorNull("unexpected state: ", ev);
                }
            }
        }
    }
}
