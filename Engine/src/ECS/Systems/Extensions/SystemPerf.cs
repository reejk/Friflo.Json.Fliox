﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using static Friflo.Engine.ECS.Systems.SystemExtensions;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Systems;

public struct SystemPerf
{
#region properties
    /// <remarks>Can be 0 in case execution time was below <see cref="Stopwatch.Frequency"/> precision.</remarks>
                    public  int     UpdateCount => updateCount;
                    public  float   LastMs      => lastTicks >= 0 ? (float)(lastTicks * StopwatchPeriodMs) : -1;
                    public  float   SumMs       => (float)(sumTicks * StopwatchPeriodMs);
    [Browse(Never)] public  long    LastTicks   => lastTicks;
    [Browse(Never)] public  long    SumTicks    => sumTicks;
    public override         string  ToString()  => $"updates: {UpdateCount}  last: {LastMs:0.###} ms  sum: {SumMs:0.###} ms";
    #endregion

#region fields
    [Browse(Never)] internal            int     updateCount;
    [Browse(Never)] internal            long    lastTicks;
    [Browse(Never)] internal            long    sumTicks;
                    internal readonly   long[]  history;
    #endregion

#region methods
    internal SystemPerf(long[] history) {
        this.history    = history;
        lastTicks       = -1;
    }
    
    public float LastAvgMs(int count)
    {
        var ticks   = history;
        var length  = ticks.Length;
        count       = Math.Min(updateCount, Math.Min(length, count));
        if (count == 0) {
            return -1;
        }
        var sum     = 0L;
        for (int n = updateCount - count; n < updateCount; n++) {
            sum += ticks[n % length];
        }
        sum /= count;
        return (float)(sum * StopwatchPeriodMs);
    }
    #endregion
}

