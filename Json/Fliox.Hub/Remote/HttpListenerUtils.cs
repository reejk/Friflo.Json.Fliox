﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Remote
{
    public static class HttpListenerUtils
    {
        public static async Task<RequestContext> ExecuteFlioxRequest (HttpListenerContext context, HttpHostHub hostHub) {
            // accepting WebSockets in Unity fails at IsWebSocketRequest. See: 
            // [Help Wanted - Websocket Server in Standalone build - Unity Forum] https://forum.unity.com/threads/websocket-server-in-standalone-build.1072526/
            //
            // Possible solutions may be like:
            // (MIT License) [ninjasource/Ninja.WebSockets: A c# implementation of System.Net.WebSockets.WebSocket for .Net Standard 2.0] https://github.com/ninjasource/Ninja.WebSockets
            // (MIT License) [sta/websocket-sharp: A C# implementation of the WebSocket protocol client and server] https://github.com/sta/websocket-sharp
            HttpListenerRequest  req  = context.Request;
#if UNITY_5_3_OR_NEWER
            if (req.Headers["Connection"] == "Upgrade" && req.Headers["Upgrade"] != null) {
                await HandleUnityServerWebSocket(context.Response).ConfigureAwait(false);
                return null;
            }
#endif
            if (req.IsWebSocketRequest) {
                var wsContext   = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                var websocket   = wsContext.WebSocket;
                await WebSocketHost.SendReceiveMessages (websocket, hostHub).ConfigureAwait(false);
                return null;
            }
            var request             = context.Request;
            var url                 = request.Url;
            var headers             = new HttpListenerHeaders(request.Headers);
            var cookies             = new HttpListenerCookies(request.Cookies);
            var requestContext      = new RequestContext(hostHub, request.HttpMethod, url.LocalPath, url.Query, req.InputStream, headers, cookies);
            await hostHub.ExecuteHttpRequest(requestContext).ConfigureAwait(false);
            return requestContext;
        }
        
        public static async Task WriteFlioxResponse(HttpListenerContext context, RequestContext requestContext) {
            if (requestContext == null)
                return; // request was WebSocket
            HttpListenerResponse resp = context.Response;
            if (!requestContext.Handled)
                return;
            var responseBody = requestContext.Response;
            SetResponseHeader(resp, requestContext.ResponseContentType, requestContext.StatusCode, responseBody.Length, requestContext.ResponseHeaders);
            await resp.OutputStream.WriteAsync(responseBody, 0, responseBody.Length).ConfigureAwait(false);
            resp.Close();
        }
        
        public static void SetResponseHeader (HttpListenerResponse response, string contentType, int statusCode, int len, Dictionary<string, string> headers) {
            response.ContentType        = contentType;
            response.ContentEncoding    = Encoding.UTF8;
            response.ContentLength64    = len;
            response.StatusCode         = statusCode;
        //  resp.Headers["link"]    = "rel=\"icon\" href=\"#\""; // not working as expected - expect no additional request of favicon.ico
            if (headers != null) {
                foreach (var header in headers) {
                    response.Headers[header.Key] = header.Value;
                }
            }
        }
        
        // ReSharper disable once UnusedMember.Local
        private static async Task HandleUnityServerWebSocket (HttpListenerResponse response) {
            const string error = "Unity HttpListener doesnt support server WebSockets";
            byte[]  resultBytes = Encoding.UTF8.GetBytes(error);
            SetResponseHeader(response, "text/plain", (int)HttpStatusCode.NotImplemented, resultBytes.Length, null);
            await response.OutputStream.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);
            response.Close();
        }
    }
    
    internal class HttpListenerHeaders : IHttpHeaders {
        private  readonly   NameValueCollection headers;
        
        public              string              this[string key] => headers[key];
        
        internal HttpListenerHeaders(NameValueCollection headers) {
            this.headers = headers;    
        }
    }
    
    internal class HttpListenerCookies : IHttpCookies {
        private  readonly   CookieCollection    cookies;
        
        public              string              this[string key] => cookies[key]?.Value;
        
        internal HttpListenerCookies(CookieCollection cookies) {
            this.cookies = cookies;    
        }
    }
}
