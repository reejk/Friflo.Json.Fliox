// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Utils;

// using ReadResolver = System.Func<Friflo.Json.Managed.JsonReader, object, Friflo.Json.Managed.Prop.NativeType, object>;



namespace Friflo.Json.Managed.Prop
{
    public abstract class NativeType : IDisposable {
        public  readonly    Type        type;
        public  readonly    IJsonCodec jsonCodec;


        public abstract Object CreateInstance();

        protected NativeType(Type type, IJsonCodec jsonCodec) {
            this.type = type;
            this.jsonCodec = jsonCodec;

        }

        public virtual void Dispose() {
        }
    }

    public class TypeNotSupported : NativeType {
        public TypeNotSupported(Type type) : 
            base(type, null) {
        }

        public override object CreateInstance() {
            throw new NotSupportedException("Type not supported. " + type.FullName);
        }
    }
    
    public class PropCollection : NativeType
    {
        public   readonly   Type            typeInterface;  // todo: remove when introducing IJsonObject.Write()
        public   readonly   Type            keyType;
        public   readonly   int             rank;
        public   readonly   Type            elementType;     // use GetElementType() if NativeType is required - its cached
        private             NativeType      elementPropType; // is set on first lookup
        public   readonly   SimpleType.Id ? id;
        internal readonly   ConstructorInfo constructor;

    
        internal PropCollection (
                Type        typeInterface,
                Type        nativeType,
                Type        elementType,
                IJsonCodec  jsonCodec,
                int         rank,
                Type        keyType) :
            base (nativeType, jsonCodec) {
            this.typeInterface  = typeInterface;
            this.keyType        = keyType;
            this.elementType    = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            this.id             = SimpleType.IdFromType(elementType);
            this.constructor    = GetConstructor (nativeType, typeInterface, keyType, elementType);
        }
        
        public NativeType GetElementType(PropType.Cache typeCache) {
            if (elementType == null)
                return null;
            // simply reduce lookups
            if (elementPropType == null)
                elementPropType = typeCache.GetType(elementType);
            return elementPropType;
        }

        public override Object CreateInstance ()
        {
            return Reflect.CreateInstance(constructor);
        }

        internal static ConstructorInfo GetConstructor (Type type, Type typeInterface, Type keyType, Type elementType)
        {
            ConstructorInfo constructor = Reflect.GetDefaultConstructor(type);
            if (constructor != null)
                return constructor;
            if  (typeInterface == typeof( Array ))
            {
                return null; // For arrays Arrays.CreateInstance(componentType, length) is used
            }
            if  (typeInterface == typeof( IList<> ))
            {
                return Reflect.GetDefaultConstructor( typeof(List<>).MakeGenericType(elementType) );
            }
            if (typeInterface == typeof( IDictionary<,> ))
            {
                return Reflect.GetDefaultConstructor( typeof(Dictionary<,>).MakeGenericType(keyType, elementType) );
            }
            throw new FrifloException ("interface type not supported");
        }

        public  class Info
        {
            internal NativeType collection;
            internal PropAccess access;
    
            public  static Info Create (FieldInfo field)
            {
                Info info = new Info();
                info.Create (field. FieldType );
                return info;
            }
    
            public  static Info Create (PropertyInfo getter)
            {
                Info info = new Info();
                info.Create (getter. PropertyType);
                return info;
            }

            public static NativeType CreateCollection (Type type)
            {
                Info info = new Info();
                info.Create (type );
                return info.collection;
            }
    
            private  void Create (Type type )
            {
                // If retrieving element type gets to buggy, change retrieving element type of collections.
                // In this case element type have to be specified via:
                // void Property.Set(String name, Class<?> entryType)
                if (type. IsArray) {
                    Type elementType = type.GetElementType();
                    int rank = type.GetArrayRank();
                    if (rank > 1) {
                        collection = new TypeNotSupported(type);
                        return;
                    }

                    SimpleType.Id? id = SimpleType.IdFromType(elementType);
                    var ar = GetArrayResolver(id);
                    collection = new PropCollection(typeof(Array), type, elementType, ar, type.GetArrayRank(), null);
                    access = new PropAccess(typeof(Array), type, elementType);
                }
                else
                {
                    Type[] args;
                    args = Reflect.GetGenericInterfaceArgs (type, typeof( IList<>) );
                    if (args != null) {
                        Type elementType = args[0];
                        collection =    new PropCollection  ( typeof( IList<>), type, elementType, ListCodec.Resolver, 1, null);
                        access =        new PropAccess      ( typeof( IList<>), type, elementType);
                    }
                    args = Reflect.GetGenericInterfaceArgs (type, typeof( IKeySet <>) );
                    if (args != null) {
                        Type elementType = args[0];
                        access = new PropAccess(typeof(IKeySet<>), type, elementType);
                    }

                    args = Reflect.GetGenericInterfaceArgs (type, typeof( IDictionary<,>) );
                    if (args != null)
                    {
                        Type elementType = args[1];
                        IJsonCodec or = MapCodec.Resolver;
                        collection =    new PropCollection  ( typeof( IDictionary<,> ), type, elementType, or, 1, args[0]);
                        access =        new PropAccess      ( typeof( IDictionary<,> ), type, elementType);
                    }
                }
            }

            IJsonCodec GetArrayResolver(SimpleType.Id? id) {
                switch (id) {
                    case SimpleType.Id.String:  return StringArrayCodec.Resolver;
                    case SimpleType.Id.Long:    return LongArrayCodec.Resolver;
                    case SimpleType.Id.Integer: return IntArrayCodec.Resolver;
                    case SimpleType.Id.Short:   return ShortArrayCodec.Resolver;
                    case SimpleType.Id.Byte:    return ByteArrayCodec.Resolver;
                    case SimpleType.Id.Bool:    return BoolArrayCodec.Resolver;
                    case SimpleType.Id.Double:  return DoubleArrayCodec.Resolver;
                    case SimpleType.Id.Float:   return FloatArrayCodec.Resolver;
                    case SimpleType.Id.Object:  return ObjectArrayCodec.Resolver;
                    default:
                        throw new NotSupportedException("unsupported array type: " + id.ToString());
                }
            }
        }


    }
}