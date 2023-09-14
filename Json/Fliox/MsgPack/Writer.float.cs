﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

// #pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

namespace Friflo.Json.Fliox.MsgPack
{

    public partial struct MsgWriter
    {
        // --- double
        public void WriteFloat64(double val) {
            var data    = Reserve(9);               // val: 9
            Write_float64_pos(data, pos, val);
        }
        
        public void WriteKeyFloat64(int keyLen, long key, double val) {
            var cur     = pos;
            var data    = Reserve(1 + 8 + 9);       // key: 1 + 8,  val: 9
            WriteKeyFix(data, cur, keyLen, key);
            Write_float64_pos(data, cur + keyLen + 1, val);
        }
        
        public void WriteKeyFloat64(int keyLen, long key, double? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyFloat64(keyLen, key, val.Value);
                return;
            }
            WriteKeyNil(keyLen, key, ref count);
        }
        
        public void WriteKeyFloat64(ReadOnlySpan<byte> key, double val) {
            var cur     = pos;
            var keyLen  = key.Length;
            var data    = Reserve(2 + keyLen + 9);  // key: 2 + keyLen,  val: 9
            WriteKeySpan(data, ref cur, key);
            Write_float64_pos(data, cur, val);
        }
        
        public void WriteKeyFloat64(ReadOnlySpan<byte> key, double? val, ref int count) {
            if (val.HasValue) {
                count++;
                WriteKeyFloat64(key, val.Value);
                return;
            }
            WriteKeyNil(key, ref count);
        }
        
        // ----------------------------------- utils ----------------------------------- 
        private void Write_float64_pos(byte[]data, int cur, double val)
        {
            data[cur]   = (byte)MsgFormat.float64;
            var bits    = BitConverter.DoubleToInt64Bits(val);
            BinaryPrimitives.WriteInt64BigEndian(new Span<byte>(data, cur + 1, 8), bits);
            pos = cur + 9;
        }
    }
}