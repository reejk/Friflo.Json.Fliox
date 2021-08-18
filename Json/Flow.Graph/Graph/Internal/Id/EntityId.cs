﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Graph.Internal.Id
{
    // -------------------------------------------- EntityId -----------------------------------------------
    internal abstract class EntityId {
        private static readonly   Dictionary<Type, EntityId> Ids = new Dictionary<Type, EntityId>();

        internal static EntityId<T> GetEntityId<T> () where T : class {
            var type = typeof(T);
            if (Ids.TryGetValue(type, out EntityId id)) {
                return (EntityId<T>)id;
            }
            var member = FindKeyMember (type);
            var property = member as PropertyInfo;
            if (property != null) {
                var result  = CreateEntityIdProperty<T>(property, type);
                Ids[type]   = result;
                return result;
            }
            var field = member as FieldInfo;
            if (field != null) {
                var result  = CreateEntityIdField<T>(field, type);
                Ids[type]   = result;
                return result;
            }
            throw new InvalidOperationException($"missing entity id member. entity: {type.Name}");
        }
        
        private static MemberInfo FindKeyMember (Type type) {
            var members = type.GetMembers(Flags);
            foreach (var member in members) {
                var customAttributes = member.CustomAttributes;
                if (IsKey(customAttributes))
                    return member;
            }
            var property = type.GetProperty("id", Flags);
            if (property != null)
                return property;
            property = type.GetProperty("Id", Flags);
            if (property != null)
                return property;
            
            var field = type.GetField("id", Flags);
            if (field != null)
                return field;
            field = type.GetField("Id", Flags);
            if (field != null)
                return field;
            
            return null;
        }

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        private static EntityId<T> CreateEntityIdProperty<T> (PropertyInfo property, Type type)  where T : class {
            var propType    = property.PropertyType;
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            if (idGetMethod == null || idSetMethod == null) {
                var msg2 = $"entity id property must have get & set: {property.Name}, type: {propType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg2);
            }
            if (propType == typeof(string)) {
                return new EntityIdStringProperty<T>(idGetMethod, idSetMethod);
            }
            if (propType == typeof(Guid)) {
                return new EntityIdGuidProperty<T>  (idGetMethod, idSetMethod);
            }
            if (propType == typeof(int)) {
                return new EntityIdIntProperty<T>  (idGetMethod, idSetMethod);
            }
            if (propType == typeof(long)) {
                return new EntityIdLongProperty<T>  (idGetMethod, idSetMethod);
            }
            // add additional types here
            var msg = $"unsupported type for entity id. property: {property.Name}, type: {propType.Name}, entity: {type.Name}";
            throw new InvalidOperationException(msg);
        }
            
        private static EntityId<T> CreateEntityIdField<T> (FieldInfo field, Type type)  where T : class {
            var fieldType = field.FieldType; 
            if (fieldType == typeof(string)) {
                return new EntityIdStringField<T>(field);
            }
            if (fieldType == typeof(Guid)) {
                return new EntityIdGuidField<T>(field);
            }
            if (fieldType == typeof(int)) {
                return new EntityIdIntField<T>(field);
            }
            if (fieldType == typeof(long)) {
                return new EntityIdLongField<T>(field);
            }
            // add additional types here
            var msg = $"unsupported type for entity id. field: {field.Name}, type: {fieldType.Name}, entity: {type.Name}";
            throw new InvalidOperationException(msg);
        }
        
        private static bool IsKey(IEnumerable<CustomAttributeData> attributes) {
            foreach (CustomAttributeData attr in attributes) {
                if (attr.AttributeType == typeof(Fri.KeyAttribute))
                    return true;
                // Unity has System.ComponentModel.DataAnnotations.KeyAttribute no available by default
                if (attr.AttributeType.FullName == "System.ComponentModel.DataAnnotations.KeyAttribute")
                    return true;
            }
            return false;
        }
        
                
        internal static Func<TEntity,TField> GetFieldGet<TEntity, TField>(FieldInfo field) {
            var instanceType    = field.DeclaringType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var fieldExp        = Expression.Field(instExp, field);
            return                Expression.Lambda<Func<TEntity, TField>>(fieldExp, instExp).Compile();
        }
        
        internal static Action<TEntity,TField> GetFieldSet<TEntity, TField>(FieldInfo field) {
            var instanceType    = field.DeclaringType;
            var fieldType       = field.FieldType;
            var instExp         = Expression.Parameter(instanceType,    "instance");
            var valueExp        = Expression.Parameter(fieldType,       "value");
            var fieldExp        = Expression.Field(instExp, field);
            var assignExpr      = Expression.Assign (fieldExp, valueExp);
            return                Expression.Lambda<Action<TEntity, TField>>(assignExpr, instExp, valueExp).Compile();
        }
    }
    
    
    // -------------------------------------------- EntityId<T> --------------------------------------------
    internal abstract class EntityId<T> : EntityId where T : class {
        internal abstract   string  GetEntityId (T entity);
        internal abstract   void    SetEntityId (T entity, string id);
    }
}