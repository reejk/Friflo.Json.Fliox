﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Hub.Remote.Transport.Udp;
using Friflo.Json.Fliox.Hub.Remote.Transport.WebRTC;
using SIPSorcery.Net;

namespace DemoTest {

    internal static class Trial
    {
        // custom entry point to run test snippets with: dotnet run
        internal static async Task Main(string[] args)
        {
            // await Benchmark.PubSubLatency();
            await QueryRelations(args);
            await SubscribeChanges();
        }
        
        private static async Task QueryRelations(string[] args)
        {
            var option      = args.FirstOrDefault() ?? "http";
            var hub         = CreateHub(option);
            var client      = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            var orders      = client.orders.QueryAll();
            var articles    = orders.ReadRelations(client.articles, o => o.items.Select(a => a.article));
            await client.SyncTasks();
                
            Console.WriteLine($"\n--- orders:");
            foreach (var order in orders.Result) {
                Console.WriteLine($"id: {order.id}, created: {order.created}");
            }
            Console.WriteLine($"\n--- articles");
            foreach (var article in articles.Result) {
                Console.WriteLine($"id: {article.id}, name: {article.name}");
            }
        }
        
        // after calling this method open: 'Hub Explorer > main_db > articles'
        // changing records in 'articles' trigger the subscription handler in this method.  
        private static async Task SubscribeChanges()
        {
            var hub         = CreateHub("webrtc");
            var client      = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            client.articles.SubscribeChanges(Change.All, (changes, context) => {
                foreach (var entity in changes.Patches) {
                    Console.WriteLine($"EventSeq: {context.EventSeq} - merge article: {entity.key}, patch: {entity.patch}");
                }
                foreach (var entity in changes.Upserts) {
                    Console.WriteLine($"EventSeq: {context.EventSeq} - upsert article: {entity.id}, name: {entity.name}");
                }
                foreach (var key in changes.Deletes) {
                    Console.WriteLine($"EventSeq: {context.EventSeq} - delete article: {key}");
                }
            });
            await client.SyncTasks();
            
            Console.WriteLine("\n wait for events ... (exit with: CTRL + C)\n note: generate events by clicking 'Save' on a record in the Hub Explorer\n");
            await Task.Delay(3_600_000); // wait 1 hour
        }
        private const string STUN_URL = "stun:stun.sipsorcery.com";
        
        static RTCConfiguration config = new RTCConfiguration {
            iceServers = new List<RTCIceServer> { new RTCIceServer { urls = STUN_URL } }
        };
            
        private static FlioxHub CreateHub(string option)
        {
            switch (option) {
                case "http":    return new HttpClientHub              ("main_db", "http://localhost:8010/fliox/");
                case "ws":      return new WebSocketClientHub         ("main_db", "ws://localhost:8010/fliox/");
                case "udp":     return new UdpSocketClientHub         ("main_db", "localhost:5000");
                case "webrtc":  return new WebRtcClientHub            ("main_db", config);
                case "file":    return new FlioxHub(new FileDatabase  ("main_db", "./DB/main_db"));
                case "memory":  return new FlioxHub(new MemoryDatabase("main_db"));
            }
            throw new InvalidOperationException($"unknown option: '{option}' use: [http, ws, udp, file, memory]");
        }
    }
}
