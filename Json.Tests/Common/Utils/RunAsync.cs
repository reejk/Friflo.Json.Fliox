﻿using System;
using System.Collections;
using System.Threading.Tasks;

namespace Friflo.Json.Tests.Common.Utils
{
    public static class RunAsync {
        public static IEnumerator Await(Task task)
        {
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) { throw task.Exception; }
        }
        
        public static IEnumerator Await(Func<Task> taskDelegate)
        {
            return Await(taskDelegate.Invoke());
        }
    }
}
    
#if UNITY_5_3_OR_NEWER
    namespace UnitTest.Dummy {
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class TestAttribute : Attribute {
            public TestAttribute () {}
        }
    }
#else
    namespace UnityEngine.TestTools {
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class UnityTest : Attribute {
            public UnityTest () {}
        }
    }
#endif


