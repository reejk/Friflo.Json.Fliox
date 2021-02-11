﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Map.Utils
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public static class ArrayUtils {
        public static bool IsNullable(ref Reader reader, TypeMapper arrayMapper, TypeMapper elementType, out bool success) {
            if (!elementType.isNullable) {
                ReadUtils.ErrorIncompatible<bool>(ref reader, arrayMapper.DataTypeName(), " element", elementType, out success);
                return false;
            }
            success = false;
            return true;
        }
        
        public static bool StartArray<TVal, TElm>(ref Reader reader, CollectionMapper<TVal, TElm> map, out bool success) {
            var ev = reader.parser.Event;
            switch (ev) {
                case JsonEvent.ValueNull:
                    if (map.isNullable) {
                        success = true;
                        return false;
                    }
                    ReadUtils.ErrorIncompatible<TVal>(ref reader, map.DataTypeName(), map, out success);
                    return default;
                case JsonEvent.ArrayStart:
                    success = true;
                    return true;
                default:
                    success = false;
                    ReadUtils.ErrorIncompatible<TVal>(ref reader, map.DataTypeName(), map, out success);
                    return false;
            }
        }

    }
}