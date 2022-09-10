﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demo;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Remote;

namespace DemoTest {

    internal static class Benchmark
    {
        internal static async Task PubSubLatency()
        {
            var hub     = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var sender  = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            
            var tickRate = 50;
            var frames   = 200;

            Console.WriteLine("                                          latency [ms] percentiles [%]");
            Console.WriteLine("clients rate frames connected  average    50    90    95    96    97    98    99   100  duration delayed");
            await PubSubLatencyCCU(sender, tickRate, frames, 2);
            await PubSubLatencyCCU(sender, tickRate, frames, 2);
            await PubSubLatencyCCU(sender, tickRate, frames, 5);
            await PubSubLatencyCCU(sender, tickRate, frames, 10);
            await PubSubLatencyCCU(sender, tickRate, frames, 50);
            Console.WriteLine();
            await PubSubLatencyCCU(sender, tickRate, frames, 100);
            await PubSubLatencyCCU(sender, tickRate, frames, 200);
            await PubSubLatencyCCU(sender, tickRate, frames, 300);
            await PubSubLatencyCCU(sender, tickRate, frames, 400);
            await PubSubLatencyCCU(sender, tickRate, frames, 500);
            Console.WriteLine();
            await PubSubLatencyCCU(sender, tickRate, frames, 1000);
            await PubSubLatencyCCU(sender, tickRate, frames, 2000);
            await PubSubLatencyCCU(sender, tickRate, frames, 3000);
            // await PubSubLatencyCCU(sender, tickRate, 4000);
            // await PubSubLatencyCCU(sender, 5000);
            //await PubSubLatencyCCU(sender, 10000);
        }
        
        const string payload_100 = "_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789_123456789";
        
        private static async Task PubSubLatencyCCU(FlioxClient sender, int tickRate, int frames, int ccu)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            Console.Write($"{ccu,7} {tickRate,4} {frames,6} ");
            var start = DateTime.Now.Ticks;
            var connectTasks = new List<Task<BenchmarkContext>>();
            for (int n = 0; n < ccu; n++) {
                var client = ConnectClient();
                connectTasks.Add(client);
            }
            var contexts = await Task.WhenAll(connectTasks);
            
            var connected = DateTime.Now.Ticks;
            
            Console.Write($"{(connected - start) / 10000,6} ms  ");
            
            // warmup
            for (int n = 0; n < 20; n++) { 
                sender.SendMessage<TestMessage>("test", null);
                await sender.SyncTasks();
                await Task.Delay(10);
            }
            var deltaTime = 1000 / tickRate;
            int delayed = 0;
            
            start = DateTime.Now.Ticks;
            var payload = payload_100;
            for (int n = 1; n <= frames; n++) {
                var testMessage = new TestMessage { start = DateTime.Now.Ticks, payload = payload};
                sender.SendMessage("test", testMessage);   // message is published to all clients
                sender.SyncTasks();
                var delay = n * deltaTime - (DateTime.Now.Ticks - start) / 10000;
                if (delay > 0) {
                    await Task.Delay((int)delay);
                } else {
                    delayed++;
                }
            }
            var duration = (DateTime.Now.Ticks - start) / 10000;
            
            await Task.Delay(100);

            var latencies = new List<double>();
            
            foreach (var c in contexts) {
                latencies.AddRange(c.latencies);
                if (c.latencies.Count != frames)
                    throw new InvalidOperationException("missing events");
            }
            
            
            // var diffs = contexts.Select(c => c.accumulatedLatency / (10000d * c.events)).ToArray();
            var p   = GetPercentiles(latencies, 100);
            var avg = latencies.Average();

            var diffStr     = $" {avg,5:0.0}  {p[50],5:0.0} {p[90],5:0.0} {p[95],5:0.0} {p[96],5:0.0} {p[97],5:0.0} {p[98],5:0.0} {p[99],5:0.0} {p[100],5:0.0} ";
            Console.WriteLine($"{diffStr}    {duration,5}   {delayed,5}");

            var tasks = new List<Task>();
            foreach (var context in contexts) {
                context.client.UnsubscribeMessage("*", null);
                var task = context.client.SyncTasks();
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            foreach (var context in contexts) {
                context.client.Dispose();
                await context.hub.Close();
                context.hub.Dispose();
            }
        }
        private static List<double> GetPercentiles(List<double> values, int count) {
            var sorted = values.OrderBy(s => s).ToList();
            if (sorted.Count < count) throw new InvalidOperationException("insufficient samples");
            var percentiles = new List<double>();
            percentiles.Add(0);
            for (int n = 0; n < count; n++) {
                var index = (int)((sorted.Count - 1) * ((n + 1) / (double)count));
                percentiles.Add(sorted[index]);
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (sorted[sorted.Count - 1] != percentiles[count])
                throw new InvalidOperationException("invalid last element");
            return percentiles;
        }
        
        private static async Task<BenchmarkContext> ConnectClient()
        {
            var hub                 = new WebSocketClientHub("main_db", "ws://localhost:8010/fliox/");
            var client              = new DemoClient(hub) { UserId = "admin", Token = "admin" };
            
            var benchmarkContext    =  new BenchmarkContext { hub = hub, client = client };
            
            client.SubscribeMessage("*", (message, context) => {
                message.GetParam(out TestMessage test, out _);
                if (test == null)
                    return;
                benchmarkContext.latencies.Add((DateTime.Now.Ticks - test.start) / 10000d);
            });
            // client.SendMessage("xxx", 111);
            
            await client.SyncTasks();
            
            return benchmarkContext;
        }
    }
    
    internal class BenchmarkContext
    {
        internal            WebSocketClientHub  hub;
        internal            FlioxClient         client;
        internal readonly   List<double>        latencies = new List<double>(); // ms
    }
    
    internal class TestMessage {
        public  long    start;
        public  string  payload;
    }
}
