// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Mapper.Utils;

namespace Friflo.Json.Mapper.Map.Obj.Class.Reflect
{
    // PropertyFields
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public sealed class PropertyFields : IDisposable
    {
        public      readonly    PropField []    fields;
        public      readonly    int             num;
        public      readonly    int             primCount;
        public      readonly    int             objCount;
    
        
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private     readonly    String              typeName;

        // ReSharper disable once UnusedMember.Local
        private static readonly Type[]                      Types = { typeof( PropCall ) };

        public PropertyFields (Type type)
        {
            typeName       = type. ToString();
            var query = new FieldQuery();
            query.SetProperties(type);
            primCount = query.primCount;
            objCount  = query.objCount;
            var fieldList = query.fieldList;
            num = fieldList. Count;
            fields = new PropField [num];
            for (int n = 0; n < num; n++)
                fields[n] = fieldList [n];
            fieldList. Clear();
        }
        
        public void Dispose() {
            for (int i = 0; i < fields.Length; i++)
                fields[i].Dispose();
        }
    }
}
