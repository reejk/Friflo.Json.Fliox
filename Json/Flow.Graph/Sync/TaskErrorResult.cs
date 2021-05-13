﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    public class TaskErrorResult : TaskResult
    {
        public              TaskErrorResultType type;
        public              string              message;

        internal override   TaskType            TaskType => TaskType.Error;
        public   override   string              ToString() => $"type: {type}, message: {message}";
    }
    
    /// <summary>Describe the type of a <see cref="TaskErrorResult"/></summary>
    public enum TaskErrorResultType {
        Undefined,          // Prevent implicit initialization of underlying value 0 to a valid value (UnhandledException)
        /// <summary>
        /// Inform about an unhandled exception in a <see cref="EntityContainer"/> implementation -> a bug.
        /// More information at <see cref="EntityDatabase.ExecuteSync"/>.
        /// </summary>
        UnhandledException,
        
        /// <summary>
        /// Inform about an error when accessing a database.
        /// E.g. the access is currently not available or accessing a missing table.
        /// </summary>
        DatabaseError
    }
}