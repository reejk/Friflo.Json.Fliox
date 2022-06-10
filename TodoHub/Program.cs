﻿using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Explorer;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using Friflo.Json.Fliox.Hub.Remote;
using Friflo.Json.Fliox.Schema.Native;

namespace Fliox.TodoHub
{
    /// <summary>Bootstrapping of databases hosted by <see cref="HttpHost"/></summary> 
    internal  static class  Program
    {
        public static void Main() {
            var hostHub = CreateHttpHost();
            HttpListenerHost.RunHost("http://+:8010/", hostHub);
        }

        /// <summary>
        /// This method is a blueprint showing how to setup a <see cref="HttpHost"/> utilizing a minimal features set
        /// available supporting HTTP and WebSockets by using a <see cref="System.Net.HttpListener"/>
        /// </summary>
        internal static HttpHost CreateHttpHost() {
            var c                   = new Config();
            var typeSchema          = NativeTypeSchema.Create(typeof(TodoStore)); // optional - create TypeSchema from Type
            var databaseSchema      = new DatabaseSchema(typeSchema);
            var database            = CreateDatabase(c, databaseSchema, null);

            var hub                 = new FlioxHub(database);
            hub.Info.projectName    = "TodoHub";                                                        // optional
            hub.AddExtensionDB (new ClusterDB("cluster", hub));     // optional - expose info of hosted databases. Required by Hub Explorer
            hub.EventDispatcher     = new EventDispatcher(true);    // optional - enables sending events for subscriptions
            
            var httpHost            = new HttpHost(hub, "/fliox/").CacheControl(c.cache);
            httpHost.AddHandler      (new StaticFileHandler(c.www).CacheControl(c.cache)); // optional - serve static web files of Hub Explorer
            return httpHost;
        }
        
        private class Config {
            internal readonly string  dbPath              = "./DB~/main_db";
            internal readonly string  www                 = HubExplorer.Path;
            internal readonly string  cache               = null; // "max-age=600"; // HTTP Cache-Control
            internal readonly bool    useMemoryDbClone    = false;
        }
        
        private static EntityDatabase CreateDatabase(Config c, DatabaseSchema schema, TaskHandler handler) {
            var fileDb = new FileDatabase("main_db", c.dbPath, handler);
            fileDb.Schema = schema;
            if (!c.useMemoryDbClone)
                return fileDb;
            // As the DemoHub is also deployed as a demo service in the internet it uses a memory database
            // to minimize operation cost and prevent abuse as a free persistent database.   
            var memoryDB = new MemoryDatabase("main_db", handler);
            memoryDB.Schema = schema;
            memoryDB.SeedDatabase(fileDb).Wait();
            return memoryDB;
        }
    }
}
