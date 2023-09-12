﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;


// ReSharper disable CommentTypo
namespace Friflo.Json.Fliox.MsgPack
{
    public partial struct MsgWriter
    {
        public int WriteArray(int length) {
            switch (length) {
                case <= 15: {
                    var data        = Reserve(1);
                    data[pos++]     = (byte)((int)MsgFormat.fixarray | length);
                    return length;
                }
                case <= ushort.MaxValue: {
                    var data        = Reserve(3);
                    var cur         = pos;
                    pos             = cur + 3; 
                    data[cur]       = (byte)MsgFormat.array16;
                    data[cur + 1]   = (byte)(length >> 8);
                    data[cur + 2]   = (byte)length;
                    return length;
                }
               default: {
                    var data = Reserve(5);
                    var cur     = pos;
                    pos         = cur + 5; 
                    data[cur]   = (byte)MsgFormat.array32;
                    BinaryPrimitives.WriteInt32BigEndian (new Span<byte>(data, cur + 1, 4), length);
                    return length;
                }
            }
        }
    }
}