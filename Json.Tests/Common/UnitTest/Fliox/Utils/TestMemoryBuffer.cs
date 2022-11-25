// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using Friflo.Json.Fliox.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Utils
{
    public static class TestMemoryBuffer
    {
        private static readonly byte[] Hello = Encoding.UTF8.GetBytes("hello");
        
        [Test]
        public static void TestMemoryBufferCapacity1() {
            MemoryStream ms = new MemoryStream();
            ms.Write(Hello);
            ms.Position = 0;
            
            var buffer = new MemoryBuffer(1);
            
            var result = ReadString(buffer, ms);

            AreEqual("hello", result);
            AreEqual(8,         buffer.Capacity);   // capacity is doubled 3 times
            AreEqual(5,         buffer.Position);
        }
        
        private static string ReadString(MemoryBuffer buffer, Stream stream) {
            buffer.Position = 0;
            int read;
            while ((read = stream.Read(buffer.GetBuffer(), buffer.Position, buffer.Remaining)) > 0)
            {
                buffer.Position += read;
                if (buffer.Remaining > 0)
                    continue;
                buffer.SetCapacity(2 * buffer.Capacity);
            }
            return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, buffer.Position);
        }
    }
}