﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Utils;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Arr
{
    public class ListMatcher : ITypeMatcher {
        public static readonly ListMatcher Instance = new ListMatcher();
        
        public TypeMapper MatchTypeMapper(Type type, StoreConfig config) {
            if (TypeUtils.IsStandardType(type)) // dont handle standard types
                return null;
            Type[] args = ReflectUtils.GetGenericInterfaceArgs (type, typeof( IList<>) );
            if (args != null) {
                Type elementType = args[0];
                ConstructorInfo constructor = ReflectUtils.GetDefaultConstructor(type);
                if (constructor == null)
                    constructor = ReflectUtils.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
                 
                object[] constructorParams = {config, type, elementType, constructor};
                // new ListMapper<IList<TElm>,TElm>  (config, type, elementType, constructor);
                var newInstance = TypeMapperUtils.CreateGenericInstance(typeof(ListMapper<,>), new[] {type, elementType}, constructorParams);
                return (TypeMapper) newInstance;
            }
            return null;
        }        
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ListMapper<TCol, TElm> : CollectionMapper<TCol, TElm> where TCol : IList<TElm>
    {
        public override string DataTypeName() { return "List"; }
        
        public ListMapper(StoreConfig config, Type type, Type elementType, ConstructorInfo constructor) :
            base(config, type, elementType, 1, typeof(string), constructor) {
        }

        public override void Write(JsonWriter writer, TCol slot) {
            int startLevel = WriteUtils.IncLevel(writer);
            var list = slot;
            writer.bytes.AppendChar('[');

            for (int n = 0; n < list.Count; n++) {
                if (n > 0)
                    writer.bytes.AppendChar(',');
                TElm item = list[n];
                
                if (!elementType.IsNull(ref item)) {
                    ObjectUtils.Write(writer, elementType, ref item);
                } else
                    WriteUtils.AppendNull(writer);
            }
            writer.bytes.AppendChar(']');
            WriteUtils.DecLevel(writer, startLevel);
        }
        

        public override TCol Read(JsonReader reader, TCol slot, out bool success) {
            if (!ArrayUtils.StartArray(reader, this, out success))
                return default;
            
            var list = slot;
            int startLen = 0;
            if (list == null)
                list = (TCol) CreateInstance();
            else
                startLen = list.Count;
            
            int index = 0;

            while (true) {

                JsonEvent ev = reader.parser.NextEvent();
                switch (ev) {
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        TElm elemVar;
                        if (index < startLen) {
                            elemVar = list[index];
                            elemVar = ObjectUtils.Read(reader, elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            list[index] = elemVar;
                        } else {
                            elemVar = default;
                            elemVar = ObjectUtils.Read(reader, elementType, ref elemVar, out success);
                            if (!success)
                                return default;
                            list.Add(elemVar);
                        }
                        index++;
                        break;
                    case JsonEvent.ValueNull:
                        if (!ArrayUtils.IsNullable(reader, this, elementType, out success))
                            return default;
                        if (index < startLen)
                            list[index] = default;
                        else
                            list.Add(default);
                        index++;
                        break;
                    case JsonEvent.ArrayEnd:
                        if (startLen - index > 0) {
                            // list.RemoveRange(index, startLen - index);
                            for (int n = startLen - 1; n >= index; n--)
                                list.RemoveAt(n); // todo check O(n)
                        }
                        success = true;
                        return list;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        ReadUtils.ErrorMsg<List<TElm>>(reader, "unexpected state: ", ev, out success);
                        return default;
                }
            }
        }
    }
}