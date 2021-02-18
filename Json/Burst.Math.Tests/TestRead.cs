﻿using NUnit.Framework;
using Unity.Mathematics;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Burst.Math.Tests
{
    public class TestRead
    {
        // Note: new properties can be added to the JSON anywhere without changing compatibility
        static readonly string jsonString = @"
{
    ""float2"":  [1,2],
    ""float3"":  [1,2,3],
    ""float4"":  [1,2,3,4],
    ""float4x4"": [
        [1.0, 2.0, 3.0, 4.0],
        [11.0, 12.0, 13.0, 14.0],
        [21.0, 22.0, 23.0, 24.0],
        [31.0, 32.0, 33.0, 34.0]
    ]
}";

        [Test]
        public void ReadMath() {
            var types   = new MathTypes();
            var keys    = new MathKeys(Default.Constructor);
            using (var parser   = new Local<JsonParser>())
            using (var json     = new Bytes(jsonString))
            {
                ref var p = ref parser.value;
                p.InitParser(json);
                p.NextEvent(); // ObjectStart

                p.IsRootObject(out JObj i);
                ReadMathTypes(ref i, ref p, in keys, ref types);

                if (p.error.ErrSet)
                    Fail(p.error.msg.ToString());
                
                AreEqual(JsonEvent.EOF, p.NextEvent()); // Important to ensure absence of application errors

                var expect = new MathTypes();
                expect.InitSample();
                
                AreEqual(expect.float2,       types.float2);
                AreEqual(expect.float3,       types.float3);
                AreEqual(expect.float4,       types.float4);
                AreEqual(expect.float4x4,     types.float4x4);
            }
        }
        
        private static void ReadMathTypes(ref JObj i, ref JsonParser p, in MathKeys k, ref MathTypes types) {
            while (i.NextObjectMember(ref p)) {
                if      (i.UseMemberFloat2  (ref p, in k.float2,    ref types.float2))     { }
                else if (i.UseMemberFloat3  (ref p, in k.float3,    ref types.float3))     { }
                else if (i.UseMemberFloat4  (ref p, in k.float4,    ref types.float4))     { }
                else if (i.UseMemberFloat4x4(ref p, in k.float4x4,  ref types.float4x4))   { }
            }
        }

    }
}