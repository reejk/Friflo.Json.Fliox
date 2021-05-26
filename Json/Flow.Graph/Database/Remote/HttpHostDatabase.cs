﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Friflo.Json.Flow.Database.Remote
{
    // [A Simple HTTP server in C#] https://gist.github.com/define-private-public/d05bc52dd0bed1c4699d49e2737e80e7
    public class HttpHostDatabase : RemoteHostDatabase
    {
        private readonly    string          endpoint;
        private readonly    HttpListener    listener;
        private             bool            runServer;
        
        private             int             requestCount;

        
        public HttpHostDatabase(EntityDatabase local, string endpoint) : base(local) {
            this.endpoint   = endpoint;
            listener        = new HttpListener();
            listener.Prefixes.Add(endpoint);
        }
        
        private async Task HandleIncomingConnections()
        {
            runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer) {
                try {
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync().ConfigureAwait(false);

                    // Peel out the requests and response objects
                    HttpListenerRequest  req  = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    string reqMsg = $@"request {++requestCount} {req.Url} {req.HttpMethod} {req.UserHostName} {req.UserAgent}";
                    Log(reqMsg);

                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if (req.HttpMethod == "POST") {
                        var inputStream = req.InputStream;
                        System.IO.StreamReader reader = new System.IO.StreamReader(inputStream, Encoding.UTF8);
                        string requestContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                        if (req.Url.AbsolutePath == "/shutdown") {
                            Log("Shutdown requested");
                            runServer = false;
                        } else {
                            byte[] data = await HandlePost(requestContent, resp).ConfigureAwait(false);
                            await resp.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                            resp.Close();
                        }
                    }
                }
#if UNITY_5_3_OR_NEWER
                catch (ObjectDisposedException  e) {
                    if (runServer)
                        Log($"RemoteHost error: {e}");
                    return;
                }
#endif
                catch (HttpListenerException  e) {
                    bool serverStopped = e.ErrorCode == 995 && runServer == false;
                    if (!serverStopped) 
                        Log($"RemoteHost error: {e}");
                    return;
                }
                catch (Exception e) {
                     Log($"RemoteHost error: {e}");
                }
            }
        }

        private async Task<byte[]> HandlePost (string requestContent, HttpListenerResponse resp) {
            var result = await ExecuteSyncJson(requestContent).ConfigureAwait(false);

            // Write the response info
            byte[] data = Encoding.UTF8.GetBytes(result.body);
            int len = data.Length;

            resp.ContentType = "application/json";
            resp.ContentEncoding = Encoding.UTF8;
            resp.ContentLength64 = len;
            resp.StatusCode = result.success ? 200 : 500;
            return data;
        }

        // For testing requires:
        //     netsh http add urlacl url=http://+:8080/ user=<DOMAIN>\<USER> listen=yes
        //     netsh http delete urlacl http://+:8080/
        // 
        // Get DOMAIN\USER via  PowerShell
        //     $env:UserName
        //     $env:UserDomain
        //
        public void Start() {
            // Create a Http server and start listening for incoming connections
            listener.Start();
            Log($"Listening for connections on {this.endpoint}");
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


        private void Log(string msg) {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }
    }
}