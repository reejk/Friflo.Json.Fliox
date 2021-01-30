﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Class.IL;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial class JsonWriter
    {
        private             int                     classLevel;
        internal readonly   bool                    useIL;
#if !UNITY_5_3_OR_NEWER
        private  readonly   List<ClassMirror>       mirrorStack = new List<ClassMirror>(16);

        private void DisposeMirrorStack() {
            for (int n = 0; n < mirrorStack.Count; n++)
                mirrorStack[n].Dispose();
        }

        private void ClearMirrorStack() {
            for (int n = 0; n < mirrorStack.Count; n++)
                mirrorStack[n].ClearObjectReferences();
        }
        
        /// <summary> Load the fields of a class instance into the <see cref="ClassMirror"/> arrays. </summary>
        internal ClassMirror InstanceLoad(TypeMapper classType, object obj) {
            if (classLevel >= mirrorStack.Count)
                mirrorStack.Add(new ClassMirror());
            var mirror = mirrorStack[classLevel++];
            mirror.LoadInstance(classType, obj);
            return mirror;
        }

        internal void InstancePop() {
            --classLevel;
        }
#else
        private     void        DisposeMirrorStack() { }
        private     void        ClearMirrorStack() { }
        internal    ClassMirror InstanceLoad(TypeMapper classType, object obj) { return null; }
        internal    void        InstancePop() { }
#endif
    }
}