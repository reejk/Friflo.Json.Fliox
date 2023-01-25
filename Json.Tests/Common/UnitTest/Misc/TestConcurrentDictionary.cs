﻿using System;
using System.Collections.Concurrent;
using Friflo.Json.Burst;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Misc
{
    public class TestConcurrentDictionary
    {
        [Test]
        public void ConcurrentDictionaryAlloc()
        {
            string abc      = "abc";
            string efg      = "efg";
            string xyz      = "xyz";
            var set = new ConcurrentDictionary<string, string> {
                [abc] = abc,
                [efg] = efg,
                [xyz] = xyz,
            };
            set.GetOrAdd("key", "key");

            var start = GC.GetAllocatedBytesForCurrentThread();
            var keyAbc = "abc";
            var valAbc = set.GetOrAdd("abc", keyAbc);

            var dif  = GC.GetAllocatedBytesForCurrentThread() - start;
            
            AreSame(valAbc, abc);
            AreEqual(0, dif);
        }

        private const int Count = 10;
        
        [Test]
        public void ConcurrentPerf()
        {
            int len = 0;
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < Count; n++) {
                var str  = new string("0123456789"); 
                len     += str.Length;
            }
            var dif  = GC.GetAllocatedBytesForCurrentThread() - start;
            Console.WriteLine($"{len}, dif: {dif}");
        }
        
        [Test]
        public void ConcurrentStringIntern()
        {
            int len     = 0;
            var key     = new BytesHash(new Bytes("0123456789"));
            var intern  = new StringIntern2();
            intern.Get(ref key);
            intern.Get(ref key);
            intern.Get(ref key);
            
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < Count; n++) {
                var str  = intern.Get(ref key);
                len     += str.Length;
            }
            var dif  = GC.GetAllocatedBytesForCurrentThread() - start;
            AreEqual(0, dif);
            Console.WriteLine($"{len}, dif: {dif}");
        }
    }
    
    class StringIntern2
    {
        private readonly ConcurrentDictionary<BytesHash, string> interns = new ConcurrentDictionary<BytesHash, string>(BytesHash.Equality);
        
        public string Get(ref BytesHash key) {
            if (interns.TryGetValue(key, out var str)) {
                return str;
            }
            str = key.value.AsString();
            interns.TryAdd(key, str);
            return str;
        }
    }
}