﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Mapper.Map;
using Friflo.Json.Mapper.Map.Obj.Class.IL;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Mapper
{
    // This class contains IL specific state/data which is used by JsonReader & JsonWriter. So its not thread safe.
    partial class JsonReader
    {
#if !UNITY_5_3_OR_NEWER
        private             int                     classLevel;
        private  readonly   List<ClassMirror>       mirrorStack = new List<ClassMirror>(16);

        private void InitMirrorStack() {
            classLevel = 0;
        }

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

        /// <summary>
        /// Store the "instances fields" represented by the <see cref="ClassMirror"/> arrays to the fields
        /// of a given class instance.
        /// </summary>
        internal void InstanceStore(ClassMirror mirror, object obj) {
            mirror.StoreInstance(obj);
            --classLevel;
        }
#else 
        private void InitMirrorStack() { }
        private void DisposeMirrorStack() { }
        private void ClearMirrorStack() { }
#endif
    }
}