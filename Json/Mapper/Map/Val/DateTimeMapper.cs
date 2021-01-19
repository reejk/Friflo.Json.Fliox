﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    public class DateTimeMapper : IJsonMapper
    {
        public static readonly DateTimeMapper Interface = new DateTimeMapper();
        
        public StubType CreateStubType(Type type) {
            if (type != typeof(DateTime))
                return null;
            return new PrimitiveType (typeof(DateTime), Interface);
        }
        
        public void Write(JsonWriter writer, ref Var slot, StubType stubType) {
            if (JsonWriter.WriteNull(writer, ref slot))
                return;
            DateTime value = (DateTime) slot.Obj;
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.bytes.AppendChar('\"');
        }

        public bool Read(JsonReader reader, ref Var slot, StubType stubType) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event == JsonEvent.ValueString) {
                if (DateTime.TryParse(value.ToString(), out DateTime ret)) {
                    slot.Obj = ret;
                    return true;
                }
                return reader.ErrorNull("Failed parsing DateTime. value: ", value.ToString());
            }
            return ValueUtils.CheckElse(reader, ref slot, stubType);
        }
    }
}
