// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Auth
{
    public class AuthClient {
        internal readonly   JsonKey                                     userId;
        internal readonly   Dictionary<EntityDatabase, RequestStats>    stats = new Dictionary<EntityDatabase, RequestStats>();
        
        public   override   string              ToString() => userId.AsString();

        internal AuthClient (in JsonKey userId) {
            this.userId     = userId;
        }
    }
}