// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Used to group commands by a single class. <br/>
    /// <br/>
    /// Command methods can be added directly to a <see cref="FlioxClient"/> sub class.
    /// When adding many commands it can cause confusion between <see cref="FlioxClient"/> own methods and the command methods.
    /// <br/>
    /// The intention is to use a sub class of <see cref="HubCommands"/> as a field in a class extending <see cref="FlioxClient"/>.
    /// This establish differentiation between <see cref="FlioxClient"/> own methods and commands added
    /// to a <see cref="FlioxClient"/> sub class.
    /// <code>
    ///     public class MyStore : FlioxClient
    ///     {
    ///         public MyCommands test;
    ///    
    ///         public MyStore(FlioxHub hub) : base(hub) {
    ///             test = new MyCommands(this);
    ///         }
    ///     }
    ///
    ///     public class MyCommands : HubCommands
    ///     {
    ///         public MyCommands(FlioxClient client) : base(client) { }
    ///    
    ///         public CommandTask &lt;string&gt; Cmd (string param) => SendCommand &lt;string, string&gt;("test.Cmd", param);
    ///     } 
    /// </code>
    /// </summary>
    public class HubCommands
    {
        private readonly    FlioxClient client;
        
        protected HubCommands (FlioxClient client) {
            this.client = client;
        }
        
        protected CommandTask<TResult> SendCommand<TParam, TResult>(string name, TParam param) {
            return client.SendCommand<TParam, TResult>(name, param);
        }
    }
    
    // ---------------------------------- standard commands ----------------------------------
    /// <summary>
    /// Contains commands addressed to the database. Its commands are prefixed with
    /// <b>std.*</b>
    /// </summary>
    public class StdCommands : HubCommands
    {
        protected internal StdCommands(FlioxClient client) : base(client) { }
        
        // Declared only to generate command in Schema 
        internal CommandTask<JsonValue>     Echo(JsonValue _) => throw new InvalidOperationException("unexpected call of DbEcho command");

        // --- commands: database
        /// <summary>echos the given parameter to assure a database is working appropriately. </summary>
        public CommandTask<TParam>          Echo<TParam> (TParam param) => SendCommand<TParam,TParam>  (Std.Echo, param);
        /// <summary>list all containers of a database</summary>
        public CommandTask<DbContainers>    Containers()=>  SendCommand<JsonValue, DbContainers>(Std.Containers,new JsonValue());
        /// <summary>list all commands exposed by a database</summary>
        public CommandTask<DbCommands>      Commands()  =>  SendCommand<JsonValue, DbCommands>  (Std.Commands,  new JsonValue());
        /// <summary>return the JSON Schema assigned to a database</summary>
        public CommandTask<DbSchema>        Schema()    =>  SendCommand<JsonValue, DbSchema>    (Std.Schema,    new JsonValue());
        /// <summary>return the number of container entities of a database</summary>
        public CommandTask<DbStats>         Stats()     =>  SendCommand<JsonValue, DbStats>     (Std.Stats,     new JsonValue());
        
        // --- commands: host
        /// <summary>returns descriptive information about a Hub</summary>
        public CommandTask<HostDetails>     Details()   =>  SendCommand<JsonValue, HostDetails> (Std.HostDetails,  new JsonValue());
        /// <summary>list all databases hosted by a Hub</summary>
        public CommandTask<HostCluster>     Cluster()   =>  SendCommand<JsonValue, HostCluster> (Std.HostCluster,  new JsonValue());
    }
    
    
    /// Should not be public. commands are prefix with
    /// <b>std.*</b>
    internal static class Std  {
        // --- database
        public const string Echo        = "std.Echo";
        public const string Containers  = "std.Containers";
        public const string Commands    = "std.Commands";
        public const string Schema      = "std.Schema";
        public const string Stats       = "std.Stats";

        // --- host
        public const string HostDetails = "std.Details";
        public const string HostCluster = "std.Cluster";
    }
}