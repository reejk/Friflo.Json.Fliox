﻿using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.Examples
{
    public class ParseJsonExample
    {
        [Test]
        public void ParseJson() {
            string jsonString = @"
{
    ""name"":       ""John"",
    ""age"":        24,
    ""hobbies"":    [""gaming"", ""star wars""]
}";
            String  name = "";
            int     age = 0;
            var     hobbies = new List<string>();
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString); 
            
            p.InitParser(json);
            if (p.NextEvent() != JsonEvent.ObjectStart)
                Fail("Expect: ObjectStart");

            ReadObject readRoot = new ReadObject();
            while (readRoot.NextEvent(ref p)) {
                if      (readRoot.UseStr(ref p, "name")) {
                    name = p.value.ToString();
                }
                else if (readRoot.UseNum(ref p, "age")) {
                    age = p.ValueAsInt(out _);
                }
                else if (readRoot.UseArr(ref p, "hobbies")) { // descend to array node
                    ReadArray readHobbies = new ReadArray();
                    while (readHobbies.NextEvent(ref p)) {
                        if (readHobbies.UseStr(ref p)) {
                            hobbies.Add(p.value.ToString());
                        }
                    }
                }
            }
            AreEqual("John",        name);
            AreEqual(24,            age);
            AreEqual("gaming",      hobbies[0]);
            AreEqual("star wars",   hobbies[1]);
        }
    }
}