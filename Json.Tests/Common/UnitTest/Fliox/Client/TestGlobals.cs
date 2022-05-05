﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.DB.Monitor;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy;
using Friflo.Json.Tests.Common.Utils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public static class TestGlobals {
        public static readonly string PocStoreFolder = CommonUtils.GetBasePath() + "assets~/DB/PocStore";
            
        
        public static SharedEnv Shared { get; private set; }
        
        public static void Init() {
            SharedTypeStore.Init();
            Shared        = new SharedEnv();
        }
        
        public static void Dispose() {
            Shared.Dispose();
            Shared = null;
            SharedTypeStore.Dispose();
        }
    }
}