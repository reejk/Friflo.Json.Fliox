﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Friflo.Json.Flow.Graph.Internal
{
    // -------------------------------------------- EntityId -----------------------------------------------
    internal abstract class EntityId {
        private static readonly   Dictionary<Type, EntityId> Ids = new Dictionary<Type, EntityId>();

        internal static EntityId<T> GetEntityId<T> () where T : class {
            var type = typeof(T);
            if (Ids.TryGetValue(type, out EntityId id)) {
                return (EntityId<T>)id;
            }
            var result  = CreateEntityId<T>("id"); 
            Ids[type]   = result;
            return result;
        }
        
        private static EntityId<T> CreateEntityId<T> (string name)  where T : class {
            var type        = typeof(T);
            var property    = type.GetProperty(name);
            if (property != null) {
                var propType = property.PropertyType; 
                if (propType == typeof(string)) {
                    return new EntityIdProperty<T>(property);
                }
                // add additional types here
                var msg = $"unsupported type for entity id. property: {name}, type: {propType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg);
            }
            var field   = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) {
                var fieldType = field.FieldType; 
                if (fieldType == typeof(string)) {
                    return new EntityIdField<T>(field);
                }
                // add additional types here
                var msg = $"unsupported type for entity id. field: {name}, type: {fieldType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg);
            }
            throw new InvalidOperationException($"entity id not found. name: {name}, entity: {type.Name}");
        }
    }
    
    
    // -------------------------------------------- EntityId<T> --------------------------------------------
    internal abstract class EntityId<T> : EntityId where T : class {
        internal abstract   string  GetEntityId (T entity);
        internal abstract   void    SetEntityId (T entity, string id);
    }
    
    
    // ------------------------------------------ EntityIdField<T> -----------------------------------------
    internal class EntityIdField<T> : EntityId<T> where T : class {
        private readonly   FieldInfo           field;
        
        internal EntityIdField(FieldInfo field) {
            this.field = field;
        }
        
        internal override   string  GetEntityId (T entity) {
            return (string)field.GetValue(entity);
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            field.SetValue(entity, id);
        }
    }
    
    
    // ----------------------------------------- EntityIdProperty<T> ---------------------------------------
    internal class EntityIdProperty<T> : EntityId<T> where T : class {
        private  readonly   Func  <T, string>   propertyGet;
        private  readonly   Action<T, string>   propertySet;
        
        internal EntityIdProperty(PropertyInfo property) {
            var idGetMethod = property.GetGetMethod(true);    
            var idSetMethod = property.GetSetMethod(true);
            propertyGet = (Func  <T, string>) Delegate.CreateDelegate (typeof(Func<T, string>),   idGetMethod);
            propertySet = (Action<T, string>) Delegate.CreateDelegate (typeof(Action<T, string>), idSetMethod);
        }
        
        internal override   string  GetEntityId (T entity){
            return propertyGet(entity);
        }
        
        internal override   void    SetEntityId (T entity, string id) {
            propertySet(entity, id);
        }
    }
}