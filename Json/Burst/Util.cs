﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Burst
{
    /**
     * Used to implement a default constructor for a struct's as C# cant have parameter less constructors for structs right now. 
     */
    public enum Default {
        Constructor // never used 
    }

    public class Allocation
    {
        public object resource;
        public StackTrace stackTrace;
    }
    
    public enum AllocType {
        Temp,
        Persistent
    }
#if JSON_BURST
    public struct AllocUtils
    {
        public static Unity.Collections.Allocator AsAllocator(AllocType allocType) {
            switch (allocType) {
                case AllocType.Persistent:
                    return Unity.Collections.Allocator.Persistent;
                case AllocType.Temp:
                    return Unity.Collections.Allocator.Temp;
            }
            // unreachable
            return default;
        }  
    }
#endif


    public static class DebugUtils
    {
        public static readonly Dictionary<object, StackTrace> Allocations = new Dictionary<object, StackTrace>();
        private static bool _enableLeakDetection;
        
        public static void TrackAllocation(object resource) {
            if (!_enableLeakDetection)
                return;
            var allocation = new Allocation();
            allocation.resource = resource;
            StackTrace stackTrace = new StackTrace(true);
            // StackFrame[] stackFrames = stackTrace.GetFrames();
            Allocations.Add(resource, stackTrace);
        }

        public static void UntrackAllocation(object resource) {
            if (!_enableLeakDetection)
                return;
            Allocations.Remove(resource);
        }

        public static void StartLeakDetection() {
            _enableLeakDetection = true;
            Allocations.Clear();
        }
        
        public static void StopLeakDetection() {
            _enableLeakDetection = false;
        }

    }
}

 
#if UNITY_5_3_OR_NEWER && !JSON_BURST
#error Burst mode disabled. If disabled this library cannot be used in Burst Jobs. Remove this line or enable Burst Jobs by adding directive JSON_BURST to: Edit > Project Settings... > Player > Other Settings > Configuration > Scripting Define Symbols  
#endif