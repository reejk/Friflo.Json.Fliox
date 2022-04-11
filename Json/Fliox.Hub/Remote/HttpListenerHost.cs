﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable MemberCanBePrivate.Global
namespace Friflo.Json.Fliox.Hub.Remote
{
    // [A Simple HTTP server in C#] https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    //
    // Note:
    // Alternatively a HTTP web server could be implemented by using Kestrel.
    // See: [Deprecate HttpListener · Issue #88 · dotnet/platform-compat] https://github.com/dotnet/platform-compat/issues/88#issuecomment-592395933
    // See: [Configure options for the ASP.NET Core Kestrel web server | Microsoft Docs] https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-5.0
    /// <summary>
    /// <see cref="HttpListenerHost"/> is a utility class used to <br/>
    ///  - handle Fliox requests <br/>
    ///  - manage the lifecycle of a <see cref="HttpListener"/>. <br/>
    /// <br/>
    /// lifecycle methods:
    /// <list type="bullet">
    ///     <item>Create an instance: <see cref="HttpListenerHost(string, HttpHostHub)"/></item>
    ///     <item>Start server: <see cref="Start"/></item>
    ///     <item>Run server loop for incoming connections: <see cref="Run"/></item>
    ///     <item>Stop server: <see cref="Stop"/></item>
    ///     <item>Shutdown server: <see cref="Dispose"/></item>
    /// </list> 
    /// </summary>
    public sealed class HttpListenerHost : IDisposable
    {
        private  readonly   HttpListener        listener;
        private             bool                runServer;
        private             int                 requestCount;
        private  readonly   HttpHostHub         hostHub;
        private  readonly   HubLogger           hubLogger;
        
        public HttpListenerHost(HttpListener httpListener, HttpHostHub hostHub) {
            this.hostHub    = hostHub;
            listener        = httpListener;
            hubLogger       = hostHub.sharedEnv.hubLogger;
        }
        
        public HttpListenerHost(string endpoint, HttpHostHub hostHub)
            : this (CreateHttpListener(new []{endpoint}), hostHub)
        { }
        
        // Note: Http server may require a permission to listen to the given host/port on Windows.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8010/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8010/
        // Get DOMAIN\USER via  PowerShell > $env:UserName / $env:UserDomain 
        public static void RunHost(string endpoint, HttpHostHub hostHub) {
            var server = new HttpListenerHost(endpoint, hostHub);
            server.Start();
            server.Run();
        }
        
        private static HttpListener CreateHttpListener(string[] endpoints) {
            var listener = new HttpListener();
            foreach (var endpoint in endpoints) {
                listener.Prefixes.Add(endpoint);
            }
            return listener;
        }

        public void Dispose() {
            listener.Close();
        }

        private async Task HandleIncomingConnections()
        {
            runServer       = true;
            var endpoint    = hostHub.endpoint;

            while (runServer) {
                try {
                    // Will wait here until we hear from a connection
                    HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
                    // await HandleListenerContext(context);            // handle incoming requests serial
                    _ = Task.Run(async () => {
                        try {
                            HttpListenerRequest  req  = context.Request;
                            if (requestCount++ == 0 || requestCount % 10000 == 0) {
                                string reqMsg = $@"request {requestCount} {req.Url} {req.HttpMethod} {req.UserAgent}"; // {req.UserHostName} 
                                LogInfo(reqMsg);
                            }
                            var path = context.Request.Url.LocalPath;
                            if (hostHub.IsMatch(path)) {
                                var response = await context.ExecuteFlioxRequest(hostHub).ConfigureAwait(false); // handle incoming requests parallel
                                
                                await context.WriteFlioxResponse(response).ConfigureAwait(false);
                                return;
                            }
                            if (path == "/" && endpoint != "/") {
                                var location = $"{endpoint}explorer/";
                                var headers = new Dictionary<string, string> { { "Location", location }};
                                await HttpListenerExtensions.WriteResponseString(context.Response, "text/plain", 302, $"redirect -> {location}", headers).ConfigureAwait(false);
                                return;
                            }
                            await HttpListenerExtensions.WriteResponseString(context.Response, "text/plain", 404, $"{path} not found", null).ConfigureAwait(false);
                        }
                        catch (Exception e) {
                            await HandleContextException(context, e).ConfigureAwait(false);
                        }
                    });
                }
#if UNITY_5_3_OR_NEWER
                catch (ObjectDisposedException  e) {
                    if (runServer)
                        LogException("HttpListenerHost - ObjectDisposedException", e);
                    return;
                }
#endif
                catch (HttpListenerException  e) {
                    bool serverStopped = e.ErrorCode == 995 && runServer == false;
                    if (!serverStopped) 
                        LogException("HttpListenerHost - HttpListenerException", e);
                    return;
                }
                catch (Exception e) {
                     LogException("HttpListenerHost - Exception", e);
                     return;
                }
            }
        }
        
        private async Task HandleContextException(HttpListenerContext context, Exception e) {
            var message = $"request failed - {e.GetType().Name}: {e.Message}";
            LogException(message, e);
            var resp    = context.Response;
            if (!resp.OutputStream.CanWrite)
                return;
            await HttpListenerExtensions.WriteResponseString(resp, "text/plain", (int)HttpStatusCode.BadRequest, message, null).ConfigureAwait(false);
            resp.Close();
        }
        
        // Http server requires setting permission to run an http server.
        // Otherwise exception is thrown on startup: System.Net.HttpListenerException: permission denied.
        // To give access see: [add urlacl - Win32 apps | Microsoft Docs] https://docs.microsoft.com/en-us/windows/win32/http/add-urlacl
        //     netsh http add urlacl url=http://+:8010/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl  http://+:8010/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain
        //
        public void Start() {
            // Create a Http server and start listening for incoming connections
            listener.Start();
            var sb = new StringBuilder();
            sb.Append("Listening for connections on:");
            foreach (var prefix in listener.Prefixes) {
                sb.Append(" ");
                sb.Append(prefix);
            }
            LogInfo(sb.ToString());
        }
        
        public void Run() {
            // Handle requests
            var listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
        }
        
        public async Task Stop() {
            await Task.Delay(1).ConfigureAwait(false);
            runServer = false;
            listener.Stop();
        }

        private void LogException(string msg, Exception exception) {
            hubLogger.Log(HubLog.Error, msg, exception);
        }

        private void LogInfo(string msg) {
            hubLogger.Log(HubLog.Info, msg);
        }
    }
}
