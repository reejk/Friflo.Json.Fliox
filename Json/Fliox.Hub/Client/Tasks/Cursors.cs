﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{

    public sealed class CloseCursorsTask : SyncTask
    {
        internal readonly   List<string>    cursors;
        
        [DebuggerBrowsable(Never)]
        internal            TaskState       state;
        internal override   TaskState       State   => state;
        internal            int             count;
        
        public   override   string          Details => $"CloseCursorsTask ()";
        public              int             Count   => IsOk("CloseCursorsTask.Count", out Exception e) ? count : throw e;


        internal CloseCursorsTask(IEnumerable<string> cursors) {
            this.cursors = cursors != null ? new List<string>(cursors) : null;
        }
    }
}

