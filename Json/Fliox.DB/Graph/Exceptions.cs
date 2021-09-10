﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Sync;

namespace Friflo.Json.Fliox.DB.Graph
{
    public class UnresolvedRefException : Exception
    {
        public readonly     string          key;
        
        internal UnresolvedRefException(string message, Type type, string key)
            : base ($"{message} Ref<{type.Name}> (key: '{key}')")
        {
            this.key = key;
        }
    }
    
    public class TaskNotSyncedException : Exception
    {
        internal TaskNotSyncedException(string message) : base (message) { }
    }
    
    public class TaskAlreadySyncedException : Exception
    {
        internal TaskAlreadySyncedException(string message) : base (message) { }
    }
    
    public class TaskResultException : Exception
    {
        public readonly     TaskError       error;
        
        internal TaskResultException(TaskError error) : base(error.GetMessage(false)) {
            this.error      = error;
        }
    }

    public class SyncResultException : Exception
    {
        public readonly     List<SyncTask>  failed;

        internal SyncResultException(ErrorResponse errorResponse, List<SyncTask> failed) : base(SyncResult.GetMessage(errorResponse, failed)) {
            this.failed = failed;
        }
    }
}