﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Mapper.Map.Obj.Class.Reflect;
using Friflo.Json.Mapper.Map.Utils;

namespace Friflo.Json.Mapper.Map.Obj.Class.IL
{
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class ClassILMapper<T> : ClassMapper<T> {

        private          ClassLayout                    layout;

        public override ClassLayout GetClassLayout() { return layout; }
        
        public ClassILMapper (Type type, ConstructorInfo constructor) :
            base (type, constructor)
        {
            layout = new ClassLayout(propFields);
        }

        public override void InitTypeMapper(TypeStore typeStore) {
            base.InitTypeMapper(typeStore);
            layout.InitClassLayout(type, propFields, typeStore.typeResolver.GetConfig());
        }
        
        public override void WriteFieldIL(JsonWriter writer, ClassMirror mirror, PropField field, int primPos, int objPos) {
            object obj = mirror.LoadObj(objPos + field.objIndex);
            if (obj == null)
                WriteUtils.AppendNull(writer);
            else
                Write(writer, (T)obj);
        }

        public override bool ReadFieldIL(JsonReader reader, ClassMirror mirror, PropField field, int primPos, int objPos) {
            T src = (T) mirror.LoadObj(objPos + field.objIndex);
            T value = Read(reader, src, out bool success);
            mirror.StoreObj(objPos + field.objIndex, value);
            return success;
        }
        
        // ----------------------------------- Write / Read -----------------------------------
    
        public override T Read(JsonReader reader, T slot, out bool success) {
            // Ensure preconditions are fulfilled
            if (!ObjectUtils.StartObject(reader, this, out success))
                return default;
                
            ref var parser = ref reader.parser;
            T obj = slot;
            TypeMapper classType = this;
            JsonEvent ev = parser.NextEvent();
            if (obj == null) {
                // Is first member is discriminator - "$type": "<typeName>" ?
                if (ev == JsonEvent.ValueString && reader.discriminator.IsEqualBytes(ref parser.key)) {
                    classType = reader.typeCache.GetTypeByName(ref parser.value);
                    if (classType == null)
                        return ReadUtils.ErrorMsg<T>(reader, "Object with discriminator $type not found: ", ref parser.value, out success);
                    ev = parser.NextEvent();
                }
                obj = (T)classType.CreateInstance();
            }

            ClassMirror mirror = reader.InstanceLoad(classType, obj);

            while (true) {
                object elemVar;
                switch (ev) {
                    case JsonEvent.ValueString:
                        PropField field = classType.GetField(ref parser.key);
                        if (field == null) {
                            if (!reader.discriminator.IsEqualBytes(ref parser.key)) // dont count discriminators
                                parser.SkipEvent();
                            break;
                        }
                        var fieldType = field.fieldType;
                        if (!fieldType.ReadFieldIL(reader, mirror, field, field.primIndex, field.objIndex))
                            return default;

                        break;
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        fieldType = field.fieldType;
                        if (field.isValueType) {
                            if (!fieldType.ReadFieldIL(reader, mirror, field, 0, 0))
                                return default;
                        } else {
                            object subRet = mirror.LoadObj(field.objIndex);
                            if (!fieldType.isNullable && subRet == null) {
                                ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                                return default;
                            }
                        }
                        break;
                    case JsonEvent.ValueNull:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        if (!field.fieldType.isNullable) {
                            ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, field.fieldType, ref parser, out success);
                            return default;
                        }
                        field.SetField(obj, null);
                        break;
                    case JsonEvent.ArrayStart:
                    case JsonEvent.ObjectStart:
                        if ((field = ObjectUtils.GetField(reader, classType)) == null)
                            break;
                        fieldType = field.fieldType;
                        if (field.isValueType) {
                            if (!fieldType.ReadFieldIL(reader, mirror, field, field.primIndex, field.objIndex))
                                return default;
                        } else {
                            object sub = mirror.LoadObj(field.objIndex);
                            object subRet = fieldType.ReadObject(reader, sub, out success);
                            if (!success)
                                return default;
                            if (!fieldType.isNullable && subRet == null) {
                                ReadUtils.ErrorIncompatible<T>(reader, "class field: ", field.name, fieldType, ref parser, out success);
                                return default;
                            }
                            mirror.StoreObj(field.objIndex, subRet);
                        }
                        break;
                    case JsonEvent.ObjectEnd:
                        reader.InstanceStore(mirror, obj);
                        success = true;
                        return obj;
                    case JsonEvent.Error:
                        success = false;
                        return default;
                    default:
                        return ReadUtils.ErrorMsg<T>(reader, "unexpected state: ", ev, out success);
                }
                ev = parser.NextEvent();
            }
        }

    }
}