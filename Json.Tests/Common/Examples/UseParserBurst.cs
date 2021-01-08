﻿using System.Collections.Generic;
using Friflo.Json.Burst;
using NUnit.Framework;

using static NUnit.Framework.Assert;

#if JSON_BURST
    using Str32 = Unity.Collections.FixedString32;
#else
    using Str32 = System.String;
#endif

namespace Friflo.Json.Tests.Common.Examples
{
    public class UseParserBurst
    {
        // Note: new properties can be added to the JSON anywhere without changing compatibility
        static readonly string jsonString = @"
{
    ""firstName"":  ""John"",
    ""age"":        24,
    ""hobbies"":    [
        {""name"":  ""Gaming"" },
        {""name"":  ""STAR WARS""}
    ],
    ""unknownMember"": { ""anotherUnknown"": 42}
}";
        public class Buddy {
            public  string       firstName;
            public  int          age;
            public  List<Hobby>  hobbies = new List<Hobby>();
        }
    
        public struct Hobby {
            public string   name;
        }
        
        public struct Keys {
            public Str32    firstName;
            public Str32    age;
            public Str32    hobbies;
            public Str32    name;

            public Keys(Default _) {
                firstName   = "firstName";
                age         = "age";
                hobbies     = "hobbies";
                name        = "name";
            }
        }
        


        /// <summary>
        /// The following JSON reader is split into multiple Read...() methods each having only one while loop to support:
        /// - good readability
        /// - good maintainability
        /// - unit testing
        /// - enables the possibility to create readable code via a code generator
        ///
        /// A weak example is shown at <see cref="UseParserMonolith"/> doing exactly the same processing. 
        /// </summary>
        [Test]
        public void ReadJson() {
            Buddy   buddy = new Buddy();
            Keys    k = new Keys(Default.Constructor);
            
            JsonParser p = new JsonParser();
            Bytes json = new Bytes(jsonString);
            try {
                p.InitParser(json);
                p.NextEvent(); // ObjectStart
                ReadBuddy(ref p, ref buddy, ref k);

                AreEqual(JsonEvent.EOF, p.NextEvent());
                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                AreEqual("John", buddy.firstName);
                AreEqual(24, buddy.age);
                AreEqual("Gaming", buddy.hobbies[0].name);
                AreEqual("STAR WARS", buddy.hobbies[1].name);
            }
            finally {
                // only required for Unity/JSON_BURST
                json.Dispose();
                p.Dispose();
            }
        }
        
        private static void ReadBuddy(ref JsonParser p, ref Buddy buddy, ref Keys k) {
            while (p.NextObjectMember()) {
                if      (p.UseMemberStr (k.firstName))    { buddy.firstName = p.value.ToString(); }
                else if (p.UseMemberNum (k.age))          { buddy.age = p.ValueAsInt(out _); }
                else if (p.UseMemberArr (k.hobbies))      { ReadHobbyList(ref p, ref buddy.hobbies, ref k); }
            }
        }
        
        private static void ReadHobbyList(ref JsonParser p, ref List<Hobby> hobbyList, ref Keys k) {
            while (p.NextArrayElement()) {
                if (p.UseElementObj()) {        
                    var hobby = new Hobby();
                    ReadHobby(ref p, ref hobby, ref k);
                    hobbyList.Add(hobby);
                }
            }
        }
        
        private static void ReadHobby(ref JsonParser p, ref Hobby hobby, ref Keys k) {
            while (p.NextObjectMember()) {
                if (p.UseMemberStr(k.name))  { hobby.name = p.value.ToString(); }
            }
        }
    }
}