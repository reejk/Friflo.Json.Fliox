﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Friflo.Json.Fliox.Mapper.Utils;

// ReSharper disable NotAccessedField.Local
namespace Friflo.Json.Fliox.Hub.Client.Internal.Map
{
    internal static class ClientEntityUtils
    {
        private static readonly Dictionary<Type, EntityInfo[]> EntityInfoCache = new Dictionary<Type, EntityInfo[]>();
        
        internal static EntityInfo[] GetEntityInfos(Type type) {
            if (EntityInfoCache.TryGetValue(type, out  EntityInfo[] result)) {
                return result;
            }
            var entityInfos = new List<EntityInfo>();
            var flags       = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo[] properties = type.GetProperties(flags);
            for (int n = 0; n < properties.Length; n++) {
                var  property       = properties[n];
                Type propType       = property.PropertyType;
                bool isEntitySet    = IsEntitySet(propType);
                if (!isEntitySet)
                    continue;
                var genericArgs = propType.GetGenericArguments();
                var info        = new EntityInfo (property.Name, propType, genericArgs[1], null, property );
                entityInfos.Add(info);
            }
            FieldInfo[] fields = type.GetFields(flags);
            for (int n = 0; n < fields.Length; n++) {
                var  field          = fields[n];
                Type fieldType      = field.FieldType;
                bool isEntitySet    = IsEntitySet(fieldType);
                if (!isEntitySet || IsAutoGeneratedBackingField(field))
                    continue;
                var genericArgs = fieldType.GetGenericArguments();
                var info        = new EntityInfo (field.Name, fieldType, genericArgs[1], field, null);
                entityInfos.Add(info);
            }
            result = entityInfos.ToArray();
            EntityInfoCache.Add(type, result);
            return result;
        }
        
        internal static Type[] GetEntityTypes<TFlioxClient>() where TFlioxClient : FlioxClient {
            var entityInfos = GetEntityInfos (typeof(TFlioxClient));
            var types       = new Type[entityInfos.Length];
            for (int n = 0; n < entityInfos.Length; n++) {
                types[n] = entityInfos[n].entityType;
            }
            return  types;
        }

        internal static bool IsEntitySet (Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntitySet<,>);
        }
        
        private static bool IsAutoGeneratedBackingField(FieldInfo field) {
            foreach (CustomAttributeData attr in field.CustomAttributes) {
                if (attr.AttributeType == typeof(CompilerGeneratedAttribute))
                    return true;
            }
            return false;
        }
    }
    
    internal readonly struct EntityInfo {
        internal readonly   string                          container;
        internal readonly   Type                            entitySetType;
        internal readonly   Type                            entityType;
        private  readonly   FieldInfo                       field;
        private  readonly   PropertyInfo                    property;
        private  readonly   Action<FlioxClient,EntitySet>   setMember;

        public override string ToString() => container;

        internal EntityInfo (
            string         container,
            Type           entitySetType,
            Type           entityType,
            FieldInfo      field,
            PropertyInfo   property)
        {
            MemberInfo member   = field;
            setMember = null;
            if (field != null) {
                // EntitySet's declared as fields are intended to be readonly.
                // => not possible to set readonly fields by expression -> create IL code instead
                setMember = DelegateUtils.CreateFieldSetter<FlioxClient,EntitySet>(field);
            } else {
                member = property;
            }
            if (property != null) {
                var exp = DelegateUtils.CreateSetLambda<FlioxClient,EntitySet>(property);
                setMember = exp.Compile();
            }
            AttributeUtils.Property(member.CustomAttributes, out string name);
            this.container      = name ?? container;
            this.entitySetType  = entitySetType;
            this.entityType     = entityType;
            this.field          = field;
            this.property       = property;
        }
        
        internal void SetEntitySetMember(FlioxClient store, EntitySet entitySet) {
            setMember(store, entitySet);
#if false
            if (field != null) {
                field.SetValue(store, entitySet);
            } else {
                property.SetValue(store, entitySet);
            }
#endif
        }
    }
}