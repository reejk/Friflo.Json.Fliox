﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Flow.Mapper.Map.Key
{
    public class LongKeyMapper : KeyMapper<long>
    {
        public override void WriteKey (ref Writer writer, long key) {
            writer.format.AppendLong(ref writer.bytes, key);
        }
        
        public override long ReadKey (ref Reader reader, out bool success) {
            ref var parser = ref reader.parser;
            return parser.valueParser.ParseLong(ref parser.key, ref parser.errVal, out success);
        }
        
        public override JsonKey     ToJsonKey      (long key) {
            return new JsonKey(key);
        }
        
        public override long      ToKey          (JsonKey key) {
            return key.AsLong();
        }
    }
}