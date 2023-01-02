// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;

namespace Friflo.Json.Fliox.Hub.Host.Accumulator
{
    internal sealed class WriteTaskModel
    {
        internal    JsonValue       task;
        internal    string          cont;
        internal    List<JsonValue> set;
        
        internal void Set(in JsonValue taskType, in SmallString container, List<JsonValue> entities) {
            task    = taskType;
            cont    = container.value;
            set     = entities;
        }
    }
    
    internal sealed class DeleteTaskModel
    {
        internal    JsonValue       task;
        internal    string          cont;
        internal    List<JsonKey>   ids;
        
        internal void Set(in JsonValue taskType, in SmallString container, List<JsonKey> keys) {
            task    = taskType;
            cont    = container.value;
            ids     = keys;
        }
    }
}