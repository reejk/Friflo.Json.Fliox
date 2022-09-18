// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

namespace Friflo.Json.Burst.Vector
{
    public class VectorOps
    {
#if   UNITY_BURST
        public static readonly VectorOps Instance = new VectorOpsUnity();
#elif XXX_NETCOREAPP3_0_OR_GREATER
        public static readonly VectorOps Instance = new VectorOpsCLR();
#else
        public static readonly VectorOps Instance = new VectorOps();
#endif

        public virtual void MaskPayload(
            byte[] dest,    int destPos,
            byte[] src,     int srcPos,
            byte[] mask,    int maskPos,
            int length)
        {
            // --- SISD
            for (int n = 0; n < length; n++) {
                var b = src[srcPos + n];
                dest[destPos + n] = (byte)(b ^ mask[(maskPos + n) % 4]);
            }
        }
        
        public virtual void Populate(byte[] arr) { }
        
        protected void PopulateVector(byte[] arr) {
            arr[4] = arr [8] = arr[12] = arr[16] = arr[0];
            arr[5] = arr [9] = arr[13] = arr[17] = arr[1];
            arr[6] = arr[10] = arr[14] = arr[18] = arr[2];
            arr[7] = arr[11] = arr[15] = arr[19] = arr[3];
        }
    }
}