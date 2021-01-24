// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Utils;

// using ReadResolver = System.Func<Friflo.Json.Managed.JsonReader, object, Friflo.Json.Managed.Prop.NativeType, object>;

namespace Friflo.Json.Mapper.Types
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class CollectionType : StubType
    {
        public   readonly   Type            keyType;
        public   readonly   int             rank;
        // ReSharper disable once UnassignedReadonlyField
        // field ist set via reflection below to enable using a readonly field
        public   readonly   StubType        elementType;
        private  readonly   Type            elementTypeNative;
        public   readonly   VarType         elementVarType;
        internal readonly   ConstructorInfo constructor;

        internal CollectionType (
            Type            type,
            Type            elementType,
            TypeMapper     map,
            int             rank,
            Type            keyType,
            ConstructorInfo constructor) : base (type, map, true)
        {
            this.keyType        = keyType;
            elementTypeNative   = elementType;
            if (elementType == null)
                throw new NullReferenceException("elementType is required");
            this.rank           = rank;
            elementVarType       = Var.GetVarType(elementType);
            // constructor can be null. E.g. All array types have none.
            this.constructor    = constructor;
        }
        
        public override void InitStubType(TypeStore typeStore) {
            FieldInfo fieldInfo = GetType().GetField("elementType");
            StubType stubType = typeStore.GetType(elementTypeNative);
            // ReSharper disable once PossibleNullReferenceException
            fieldInfo.SetValue(this, stubType);
        }

        
        /*
        public NativeType GetElementType(TypeCache typeCache) {
            if (elementType == null)
                return null;
            // simply reduce lookups
            if (elementPropType == null)
                elementPropType = typeCache.GetType(elementType);
            return elementType;
        } */

        public override Object CreateInstance ()
        {
            return Reflect.CreateInstance(constructor);
        }
    }
}