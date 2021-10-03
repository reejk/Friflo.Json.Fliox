﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Client.Internal.KeyRef
{
    // -------------------------------------------- RefKey -----------------------------------------------
    internal abstract class RefKey {
        private static readonly   Dictionary<Type, RefKey> Map = new Dictionary<Type, RefKey>();

        internal static RefKey<TKey, T> GetRefKey<TKey, T> () where T : class {
            var type = typeof(Ref<TKey,T>);
            if (Map.TryGetValue(type, out RefKey id)) {
                return (RefKey<TKey, T>)id;
            }
            var result  = CreateRefKey<TKey, T>();
            Map[type]   = result;
            return (RefKey<TKey, T>)result;
        }

        private static RefKey CreateRefKey<TKey, T> () where T : class {
            var type    = typeof (T);
            var keyType = typeof (TKey);
            if (keyType == typeof(string))  return new RefKeyString<T>   ();
            if (keyType == typeof(Guid))    return new RefKeyGuid<T>     ();
            if (keyType == typeof(Guid?))   return new RefKeyGuidNull<T> ();
            if (keyType == typeof(int))     return new RefKeyInt<T>      ();
            if (keyType == typeof(int?))    return new RefKeyIntNull<T>  ();
            if (keyType == typeof(long))    return new RefKeyLong<T>     ();
            if (keyType == typeof(long?))   return new RefKeyLongNull<T> ();
            if (keyType == typeof(short))   return new RefKeyShort<T>    ();
            if (keyType == typeof(short?))  return new RefKeyShortNull<T>();
            if (keyType == typeof(byte))    return new RefKeyByte<T>     ();
            if (keyType == typeof(byte?))   return new RefKeyByteNull<T> ();
            if (keyType == typeof(JsonKey)) return new RefKeyJsonKey<T>  ();
            var msg = UnsupportedTypeMessage(keyType, type);
            throw new InvalidOperationException(msg);
        }

        private static string UnsupportedTypeMessage(Type keyType, Type type) {
            return $"unsupported TKey Type: Ref<{keyType.Name},{type.Name}>";
        }
    }

    // ------------------------------------------ RefKey<TKey, T> ------------------------------------------
    internal abstract class RefKey<TKey, T> : RefKey {
        internal abstract   JsonKey KeyToId (in TKey key);
        internal abstract   TKey    IdToKey (in JsonKey key);
        
        internal virtual    bool    IsKeyNull (TKey key) => false;
    }
}