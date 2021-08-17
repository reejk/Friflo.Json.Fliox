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

        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        private static EntityId<T> CreateEntityId<T> (string name)  where T : class {
            var type        = typeof(T);
            var property    = type.GetProperty(name, Flags);
            if (property != null) {
                var propType    = property.PropertyType;
                var idGetMethod = property.GetGetMethod(true);    
                var idSetMethod = property.GetSetMethod(true);
                if (idGetMethod == null || idSetMethod == null) {
                    var msg2 = $"entity id property must have get & set: {name}, type: {propType.Name}, entity: {type.Name}";
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
                var msg = $"unsupported type for entity id. property: {name}, type: {propType.Name}, entity: {type.Name}";
                throw new InvalidOperationException(msg);
            }
            var field   = type.GetField(name, Flags);
            if (field != null) {
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
}