// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Mapper.Utils;
#if JSON_BURST
    using Str128 = Unity.Collections.FixedString128;
#else
    using Str128 = System.String;
    // ReSharper disable InconsistentNaming
#endif


namespace Friflo.Json.Burst
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    // Intended to be passed as ref parameter to be able notify a possible error 
    public struct JsonError : IDisposable
    {
        public          bool        throwException; // has only effect in managed code
        public          bool        ErrSet  { get; private set; }
        public          Bytes       msg;
        public          int         Pos     { get; private set; }

        public void InitJsonError(int capacity) {
            msg.InitBytes(capacity);
        }

        public void Dispose() {
            msg.Dispose();
        }

        public bool Error (int pos) {
            ErrSet = true;
            Pos = pos;
#if !JSON_BURST
            if (throwException)
                throw new FrifloException (msg.ToString());
#endif
            return false;
        }

        public void Clear() {
            ErrSet = false;
            msg.Clear();
        }
    
        public override String ToString () {
            return msg.ToString();
        }   
    }
}