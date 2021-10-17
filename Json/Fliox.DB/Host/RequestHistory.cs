// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Friflo.Json.Fliox.DB.Host
{
    internal class RequestHistories
    {
        private readonly    List<RequestHistory>    histories = new List<RequestHistory>();
        private readonly    Stopwatch               watch = new Stopwatch();
        
        internal RequestHistories() {
            histories.Add(new RequestHistory(1, 10));
            watch.Start();
        }
        
        internal void Update() {
            long elapsed = watch.ElapsedMilliseconds / 1000;
            foreach (var history in histories) {
                var counters    = history.counters;
                int size        = counters.Length;
                var index       = (int)((elapsed / history.resolution) % size);
                if (history.lastIndex == index) {
                    counters[index]++;
                    continue;
                }
                var clearIndex = (int)(history.lastIndex + 1) % size;
                while (clearIndex != index) {
                    counters[clearIndex] = 0;
                    clearIndex = (clearIndex + 1) % size;
                }
                counters[index] = 1;
                history.lastIndex = index;
            }
            // foreach (var history in histories) { Console.Out.WriteLine(string.Join(", ", history.counters)); }
        }
    }
    
    internal class RequestHistory {
        internal readonly   int     resolution;  // [second]
        internal readonly   int[]   counters;
        internal            long    lastIndex;
        
        internal RequestHistory (int resolution, int size) {
            this.resolution = resolution;
            counters         = new int[size];
        }
    }
}