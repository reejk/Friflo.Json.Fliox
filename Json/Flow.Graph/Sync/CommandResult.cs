﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using Friflo.Json.Flow.Mapper;

namespace Friflo.Json.Flow.Sync
{
    public interface ICommandResult
    {
        [Fri.Property(Name = "error")]
        DatabaseError               Error { get; set;  }
    }
    
    public class DatabaseError
    {
        public          string      message;

        public override string      ToString() => message;
    }
}