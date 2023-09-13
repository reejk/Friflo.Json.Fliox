﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Buffers.Binary;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;
using static Friflo.Json.Fliox.MsgPack.MsgReaderState;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace Friflo.Json.Fliox.MsgPack
{
    [Flags]
    public enum MsgReaderState
    {
        Ok                      = 0,
        // 
        RangeError              = 0b1000_0000, // bit set for Expect* values
        //
        Mask                    = 0x7f,
        //
        UnexpectedEof           = 0x10,
        UnsupportedType         = 0x20,
        // --- expected state / error
        //
        ExpectArray             = 0x01,
        ExpectByteArray         = 0x02,
        ExpectBool              = 0x03,
        ExpectString            = 0x04,
        ExpectObject            = 0x05,
        ExpectKeyString         = 0x06,
        //
        ExpectUint8             = 0x07,
        ExpectInt16             = 0x08,
        ExpectInt32             = 0x09,
        ExpectInt64             = 0x0a,
        ExpectFloat32           = 0x0b,
        ExpectFloat64           = 0x0c,
    }

    public ref partial struct MsgReader
    {
                        private     ReadOnlySpan<byte>  data;
        [Browse(Never)] private     int                 pos;
        [Browse(Never)] private     ReadOnlySpan<byte>  keyName;
        [Browse(Never)] private     MsgReaderState      state;
        [Browse(Never)] private     MsgFormat           errorType;
        [Browse(Never)] private     int                 errorPos;
        
                        public      int                 Pos             => pos;
        /// <summary><see cref="keyName"/> is set in <see cref="ReadKey"/></summary>
                        public      ReadOnlySpan<byte>  KeyName         => keyName;
                        public      string              KeyNameString   => keyName == null ? null : MsgPackUtils.SpanToString(keyName);
        
                        public      string              Error           => CreateErrorMessage();
                        public      MsgReaderState      State           => state;
                        public      override string     ToString()      => GetString();

                        private     const int           MsgError = int.MaxValue;
        
        public MsgReader(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = Ok;
            errorType   = MsgFormat.root;
            errorPos    = 0;
        }
        
        public void Init(ReadOnlySpan<byte> data) {
            this.data   = data;
            pos         = 0;
            keyName     = default;
            state       = Ok;
            errorType   = MsgFormat.root;
            errorPos    = 0;
        }
        
        /// <summary>
        /// Return true if the given <paramref name="key"/> is equal to <see cref="KeyName"/>
        /// </summary>
        public bool IsKeyEquals(byte[] key) {
            return keyName.SequenceEqual(key);
        }
        
        private string GetString() {
            if (state != Ok) {
                return CreateErrorMessage();
            }
            var sb = new StringBuilder();
            sb.Append($"pos: {pos} ");
            if (keyName == null) {
                sb.Append("(root)");
            } else {
                sb.Append($", last key: '{KeyNameString}'");
            }
            return sb.ToString();
        }
        
        public bool ReadBool ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return false;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case MsgFormat.True:    pos = cur + 1; return true;
                case MsgFormat.False:   pos = cur + 1; return false;
            }
            SetError(ExpectBool, type, cur);
            return false;
        }
        
        public string ReadString ()
        {
            int cur = pos;
            if (cur >= data.Length) {
                SetEofError(cur);
                return null;
            }
            var type = (MsgFormat)data[cur];
            switch (type) {
                case MsgFormat.nil:
                    pos = cur + 1;
                    return null;
                case >= MsgFormat.fixstr and <= MsgFormat.fixstrMax: {
                    if (!read_str(out var span, cur + 1, (int)type & 0x1f, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
                case MsgFormat.str8: {
                    if (cur + 1 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return null;
                    }
                    int len = data[cur + 1];
                    if (!read_str(out var span, cur + 2, len, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
                case MsgFormat.str16: {
                    if (cur + 2 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return null;
                    }
                    int len = data[cur + 1] << 8 | data [cur + 2];
                    if (!read_str(out var span, cur + 3, len, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
                case MsgFormat.str32: {
                    if (cur + 4 >= data.Length) {
                        SetEofErrorType(type, cur);
                        return null;
                    }
                    var len = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(cur + 1, 4));
                    if (!read_str(out var span, cur + 5, (int)len, type)) {
                        return null;
                    }
                    return MsgPackUtils.SpanToString(span);
                }
            }
            SetError(ExpectString, type, cur);
            return null;
        }
    }
}