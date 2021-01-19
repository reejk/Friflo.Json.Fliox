﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Val
{
    public class EnumMapper : IJsonMapper
    {
        public static readonly EnumMapper Interface = new EnumMapper();
        
        public StubType CreateStubType(Type type) {
            bool isNullable = false;
            if (!type.IsEnum) {
                Type[] args = Reflect.GetGenericInterfaceArgs (type, typeof( Nullable<>) );
                if (args == null)
                    return null;
                Type nullableType = args[0];
                if (!nullableType.IsEnum)
                    return null;
                isNullable = true;
            }
            return new EnumType (type, Interface, isNullable);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            EnumType enumType = (EnumType) stubType;
            if (enumType.enumToString.TryGetValue((Enum)slot.Obj, out BytesString enumName)) {
                writer.bytes.AppendChar('\"');
                writer.bytes.AppendBytes(ref enumName.value);
                writer.bytes.AppendChar('\"');
            }
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            EnumType enumType = (EnumType) stubType;
            ref var parser = ref reader.parser;
            if (parser.Event == JsonEvent.ValueString) {
                reader.bytesRef.value = parser.value;
                if (enumType.stringToEnum.TryGetValue(reader.bytesRef, out Enum enumValue)) {
                    slot.Obj = enumValue;
                    return true;
                }
                return reader.ErrorIncompatible("enum value", stubType, ref parser);
            }
            if (parser.Event == JsonEvent.ValueNumber) {
                long integralValue = parser.ValueAsLong(out bool success);
                if (!success)
                    return false;
                if (enumType.integralToEnum.TryGetValue(integralValue, out Enum enumValue)) {
                    slot.Obj = enumValue;
                    return true;
                }
                return reader.ErrorIncompatible("enum value", stubType, ref parser);
            }
            return ValueUtils.CheckElse(reader, stubType);
        }
    }
}
