﻿using System;
using Friflo.Json.Burst;
using NUnit.Framework;

namespace Friflo.Json.Tests.Common.UnitTest
{
#if !UNITY_5_3_OR_NEWER  // no clean up of native containers for Unity/JSON_BURST
    
    public class TestHelloWorld
    {
        [Test]
        public void HelloWorldParser() {
            string say = "", to = "";
            var p = new JsonParser();
            p.InitParser(new Bytes (@"{""say"": ""Hello 👋"", ""to"": ""World 🌍""}"));
            p.NextEvent();
            var i = p.GetObjectIterator();
            while (p.NextObjectMember(ref i)) {
                if (p.UseMemberStr(ref i, "say"))  { say = p.value.ToString(); }
                if (p.UseMemberStr(ref i, "to"))   { to =  p.value.ToString(); }
            }
            Console.WriteLine($"{say}, {to}"); // your console may not support Unicode
        }
        
        [Test]
        public void HelloWorldSerializer() {
            var s = new JsonSerializer();
            s.InitSerializer();
            s.ObjectStart();
            s.MemberStr("say", "Hello 👋");
            s.MemberStr("to",  "World 🌍");
            s.ObjectEnd();
            Console.WriteLine(s.dst.ToString()); // your console may not support Unicode
        }
    }
#endif
}