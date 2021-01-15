// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Friflo.Json.Burst;
using Friflo.Json.Managed.Codecs;
using Friflo.Json.Managed.Utils;

namespace Friflo.Json.Managed.Types
{
    // PropType
    public class ClassType : StubType
    {
        private readonly FFMap<String, PropField>   strMap      = new HashMapOpen<String, PropField>(13);
        private readonly FFMap<Bytes, PropField>    fieldMap    = new HashMapOpen<Bytes,  PropField>(11);
        public  readonly PropertyFields             propFields;
        private readonly ConstructorInfo            constructor;
        
        
        public override void Dispose() {
            base.Dispose();
            propFields.Dispose();
        }

        // PropType
        internal ClassType (Type type, IJsonCodec codec, ConstructorInfo constructor) :
            base (type, codec, true)
        {
            propFields = new  PropertyFields (type, this, true, true);
            for (int n = 0; n < propFields.num; n++)
            {
                PropField   field = propFields.fields[n];
                if (strMap.Get(field.name) != null)
                    throw new InvalidOperationException("assert field is accessible via string lookup");
                strMap.Put(field.name, field);
                fieldMap.Put(field.nameBytes, field);
            }
            this.constructor = constructor;
        }

        public override void InitStubType(TypeStore typeStore) {
            for (int n = 0; n < propFields.num; n++) {
                PropField field = propFields.fields[n];

                field.FieldType = typeStore.GetType(field.fieldTypeNative);
                field.collectionConstructor  = field.FieldType is CollectionType propCollection ? propCollection.constructor : null;
            }
        }
        
        public override Object CreateInstance()
        {
            if (constructor == null) {
                // Is it a struct?
                if (type.IsValueType)
                    return Activator.CreateInstance(type);
                throw new FrifloException("No default constructor available for: " + type.Name);
            }
            return Reflect.CreateInstance(constructor);
        }

        public PropField GetField (String name)
        {
            return strMap.Get(name);
        }

        public PropField GetField (Bytes fieldName)
        {
            return fieldMap.Get(fieldName);
        }
        

    }   
}
