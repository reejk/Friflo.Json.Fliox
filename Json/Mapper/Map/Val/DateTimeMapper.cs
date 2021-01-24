﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Types;

namespace Friflo.Json.Mapper.Map.Val
{
    
    public class DateTimeMatcher  : ITypeMatcher {
        public static readonly DateTimeMatcher Instance = new DateTimeMatcher();

        public ITypeMapper CreateStubType(Type type) {
            if (type != typeof(DateTime))
                return null;
            return new DateTimeMapper (type);
        }
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DateTimeMapper : TypeMapper<DateTime>
    {
        public override string DataTypeName() { return "DateTime"; }
        
        public DateTimeMapper(Type type) :
            base (type, PrimitiveUtils.IsPrimitiveNullable(type)) {
        }

        public override void Write(JsonWriter writer, DateTime value) {
            writer.bytes.AppendChar('\"');
            writer.bytes.AppendString(value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            writer.bytes.AppendChar('\"');
        }

        public override DateTime Read(JsonReader reader, DateTime slot, out bool success) {
            ref var value = ref reader.parser.value;
            if (reader.parser.Event == JsonEvent.ValueString) {
                if (DateTime.TryParse(value.ToString(), out slot)) {
                    success = true;
                    return slot;
                }
                return ReadUtils.ErrorMsg<DateTime>(reader, "Failed parsing DateTime. value: ", value.ToString(), out success);
            }
            return ValueUtils.CheckElse(reader, this, out success);
        }
    }
}
