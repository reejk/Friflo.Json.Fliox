﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;

// #pragma warning disable CS3001  // Argument type 'ulong' is not CLS-compliant

namespace Friflo.Json.Fliox.MsgPack
{

    public partial struct MsgWriter
    {
        private void Write_byte(byte[]data, int cur, byte val)
        {
            switch (val)
            {
                case > (int)sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = val;
                    pos = cur + 2;
                    return;
                default:
                    data[cur]   = val;
                    pos = cur + 1;
                    return;
            }
        }
        
        private void Write_short(byte[]data, int cur, short val)
        {
            switch (val)
            {
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint16;
                    BinaryPrimitives.WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)val);
                    pos = cur + 3;
                    return;
                case > sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= 0:
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                // --------------------------------- val < 0  ---------------------------------
                case >= -32:
                    data[cur] = (byte)(0xe0 | val);
                    pos = cur + 1;
                    return;
                case >= sbyte.MinValue:
                    data[cur]       = (byte)MsgFormat.int8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= short.MinValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    BinaryPrimitives.WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
            }
        }
        
        private void Write_int(byte[]data, int cur, int val)
        {
            switch (val)
            {
                case > ushort.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint32;
                    BinaryPrimitives.WriteUInt32BigEndian (new Span<byte>(data, cur + 1, 4), (uint)val);
                    pos = cur + 5;
                    return;
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint16;
                    BinaryPrimitives.WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)val);
                    pos = cur + 3;
                    return;
                case > sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= 0:
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                // --------------------------------- val < 0  ---------------------------------
                case >= -32:
                    data[cur] = (byte)(0xe0 | val);
                    pos = cur + 1;
                    return;
                case >= sbyte.MinValue:
                    data[cur]       = (byte)MsgFormat.int8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= short.MinValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    BinaryPrimitives.WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
                case >= int.MinValue:
                    data[cur]       = (byte)MsgFormat.int32;
                    BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), (int)val);
                    pos = cur + 5;
                    return;
            }
        }
        
        private void Write_long(byte[]data, int cur, long val)
        {
            switch (val)
            {
                case > uint.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint64;
                    BinaryPrimitives.WriteUInt64BigEndian (new Span<byte>(data, cur + 1, 8), (ulong)val);
                    pos = cur + 9;
                    return;
                case > ushort.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint32;
                    BinaryPrimitives.WriteUInt32BigEndian (new Span<byte>(data, cur + 1, 4), (uint)val);
                    pos = cur + 5;
                    return;
                case > byte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint16;
                    BinaryPrimitives.WriteUInt16BigEndian (new Span<byte>(data, cur + 1, 2), (ushort)val);
                    pos = cur + 3;
                    return;
                case > sbyte.MaxValue:
                    data[cur]       = (byte)MsgFormat.uint8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= 0:
                    data[cur]   = (byte)val;
                    pos = cur + 1;
                    return;
                // --------------------------------- val < 0  ---------------------------------
                case >= -32:
                    data[cur] = (byte)(0xe0 | val);
                    pos = cur + 1;
                    return;
                case >= sbyte.MinValue:
                    data[cur]       = (byte)MsgFormat.int8;
                    data[cur + 1]   = (byte)val;
                    pos = cur + 2;
                    return;
                case >= short.MinValue:
                    data[cur]       = (byte)MsgFormat.int16;
                    BinaryPrimitives.WriteInt16BigEndian (new Span<byte>(data, cur + 1, 2), (short)val);
                    pos = cur + 3;
                    return;
                case >= int.MinValue:
                    data[cur]       = (byte)MsgFormat.int32;
                    BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), (int)val);
                    pos = cur + 5;
                    return;
                case >= long.MinValue:
                    data[cur]       = (byte)MsgFormat.int64;
                    BinaryPrimitives.WriteInt64BigEndian (new Span<byte>(data, cur + 1, 8), val);
                    pos = cur + 9;
                    return;
            }
        }
    }
}