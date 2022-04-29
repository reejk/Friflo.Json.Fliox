// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.IO;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable MethodHasAsyncOverload
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Remote
{
    public partial class TestRemote
    {
        /// <summary>
        /// use name <see cref="Rest__main_db_init"/> to run as first test.
        /// This forces loading all required RestHandler code and subsequent tests show the real execution time
        /// </summary>
        [Test, Order(0)]
        public static void Rest__main_db_init() {
            var request = RestRequest("POST", "/rest/main_db/", "?command=std.Echo");
            AssertRequest(request, 200, "application/json", "null");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_happy_read() {
            ExecuteRestFile("Rest/main_db/happy-read.http", "Rest/main_db/happy-read.result.http");
        }
        
        [Test, Order(1)]
        public static void Rest_main_db_errors() {
            ExecuteRestFile("Rest/main_db/errors.http", "Rest/main_db/errors.result.http");
        }
        
        [Test, Order(2)]
        public static void Rest_main_db_happy_mutate() {
            ExecuteRestFile("Rest/main_db/happy-mutate.http", "Rest/main_db/happy-mutate.result.http");
        }
        
        // --------------------------------------- utils ---------------------------------------
        private static readonly string RestAssets = CommonUtils.GetBasePath() + "assets~/Rest/";
        
        private static string ReadRestRequest(string path) {
            return File.ReadAllText(RestAssets + path);
        }

        private static void WriteRestResponse(RequestContext request, string path) {
            File.WriteAllBytes(RestAssets + path, request.Response.AsByteArray());
        }
    }
}
