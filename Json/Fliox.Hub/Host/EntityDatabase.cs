// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.DB.Cluster;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Friflo.Json.Fliox.Utils;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host
{
    public sealed class DbOpt {
        /// <see cref="EntityDatabase.customContainerName"/>
        public  readonly    CustomContainerName customContainerName;
        
        public DbOpt(CustomContainerName customContainerName = null) {
            this.customContainerName    = customContainerName   ?? (name => name);
        }
        
        internal static readonly DbOpt Default = new DbOpt();
    }
    
    public delegate string CustomContainerName(string name);
    
    /// <summary>
    /// <see cref="EntityDatabase"/> is the abstraction for specific database adapter / implementation e.g. a
    /// <see cref="MemoryDatabase"/> or <see cref="FileDatabase"/>.
    /// </summary>
    /// <remarks>
    /// An <see cref="EntityDatabase"/> contains multiple <see cref="EntityContainer"/>'s each representing
    /// a table / collection of a database. Each container is intended to store the records / entities of a specific type.
    /// E.g. one container for storing JSON objects representing 'articles' another one for storing 'orders'.
    /// <br/>
    /// Optionally a <see cref="DatabaseSchema"/> can be assigned to the database via the property <see cref="Schema"/>.
    /// This enables Type / schema validation of JSON entities written (create, update and patch) to its containers.
    /// <br/>
    /// Instances of <see cref="EntityDatabase"/> and all its implementation are designed to be thread safe enabling multiple
    /// clients e.g. <see cref="Client.FlioxClient"/> operating on the same <see cref="EntityDatabase"/> instance
    /// - used by a <see cref="FlioxHub"/>.
    /// To maintain thread safety <see cref="EntityDatabase"/> implementations must not have any mutable state.
    /// </remarks>
    public abstract class EntityDatabase : IDisposable
    {
    #region - members
        /// <summary>database name</summary>
        public   readonly   string              name;       // non null
        public   override   string              ToString()  => name;
        
        /// <summary> map of of containers identified by their container name </summary>
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<string, EntityContainer>   containers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EntityContainer>                    Containers => containers.Values;
        
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema      Schema          { get; set; }
        
        /// <summary>A mapping function used to assign a custom container name.</summary>
        /// <remarks>
        /// If using a custom name its value is assigned to the containers <see cref="EntityContainer.instanceName"/>. 
        /// By having the mapping function in <see cref="EntityContainer"/> it enables uniform mapping across different
        /// <see cref="EntityContainer"/> implementations.
        /// </remarks>
        public   readonly   CustomContainerName customContainerName;
        
        /// <summary>
        /// The <see cref="service"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// An <see cref="EntityDatabase"/> implementation can assign as custom handler by its constructor
        /// </summary>
        internal readonly   DatabaseService     service;    // never null
        /// <summary>name of the storage type. E.g. <c>in-memory, file-system, remote, Cosmos, ...</c></summary>
        public   abstract   string              StorageType  { get; }
        #endregion
        
    #region - initialize
        /// <summary>
        /// constructor parameters are mandatory to force implementations having them in their constructors also or
        /// pass null by implementations.
        /// </summary>
        protected EntityDatabase(string dbName, DatabaseService service, DbOpt opt){
            containers          = new ConcurrentDictionary<string, EntityContainer>();
            this.name           = dbName ?? throw new ArgumentNullException(nameof(dbName));
            customContainerName = (opt ?? DbOpt.Default).customContainerName;
            this.service        = service ?? new DatabaseService();
        }
        
        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        #endregion
        
    #region - general public methods
        /// <summary>Create a container with the given <paramref name="name"/> in the database</summary>
        public abstract EntityContainer CreateContainer     (string name, EntityDatabase database);
        
        internal void AddContainer(EntityContainer container) {
            containers.TryAdd(container.name, container);
        }
        
        protected bool TryGetContainer(string name, out EntityContainer container) {
            return containers.TryGetValue(name, out container);
        }

        /// <summary>
        /// return the <see cref="EntityContainer"/> with the given <paramref name="name"/>.
        /// Create a new <see cref="EntityContainer"/> if not already done.
        /// </summary>
        public EntityContainer GetOrCreateContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        protected virtual Task<string[]> GetContainers() {
            var containerList = new string[containers.Count];
            int n = 0;
            foreach (var container in containers) {
                containerList[n++] = container.Key;
            }
            return Task.FromResult(containerList);
        }
        
        /// <summary>return all database containers</summary>
        public async Task<DbContainers> GetDbContainers() {
            string[] containerList;
            var schema = Schema;
            if (schema != null) {
                containerList = schema.GetContainers();
            } else {
                containerList = await GetContainers().ConfigureAwait(false);
            }
            return new DbContainers { containers = containerList, storage = StorageType };
        }

        private static class Static {
            internal const bool ExposeSchemaCommands = true; // false for debugging
        }

        /// <summary>return all database messages and commands</summary>
        public DbMessages GetDbMessages() {
            string[] commands;
            string[] messages;
            var schema = Schema;
            if (Static.ExposeSchemaCommands && schema != null) {
                commands = schema.GetCommands();
                messages = schema.GetMessages();
            } else {
                commands = service.GetCommands();
                messages = service.GetMessages();
            }
            return new DbMessages { commands = commands, messages = messages };
        }
        #endregion

    #region - seed database
        /// <summary>Seed the database with content of the given <paramref name="src"/> database</summary>
        /// <remarks>
        /// If given database has no schema the key name of all entities in all containers need to be "id"
        /// </remarks>
        public async Task SeedDatabase(EntityDatabase src) {
            var sharedEnv       = new SharedEnv();
            var pool            = sharedEnv.Pool;
            var memoryBuffer    = new MemoryBuffer();
            var syncContext     = new SyncContext(pool, null, sharedEnv.sharedCache, memoryBuffer);
            var containerNames  = await src.GetContainers().ConfigureAwait(false);
            var entityTypes     = src.Schema?.typeSchema.GetEntityTypes();
            foreach (var container in containerNames) {
                string keyName = null;
                if (entityTypes != null && entityTypes.TryGetValue(container, out TypeDef entityType)) {
                    keyName = entityType.KeyField;
                }
                await SeedContainer(src, container, keyName, syncContext).ConfigureAwait(false);
                
                memoryBuffer.DebugClear();
                memoryBuffer.Reset();
            }
        }
        
        private async Task SeedContainer(EntityDatabase src, string container, string keyName, SyncContext syncContext)
        {
            var srcContainer    = src.GetOrCreateContainer(container);
            var dstContainer    = GetOrCreateContainer(container);
            var filterContext   = new OperationContext();
            filterContext.Init(Operation.FilterTrue, out _);
            var query           = new QueryEntities { container = container, filterContext = filterContext, keyName = keyName };
            var queryResult     = await srcContainer.QueryEntities(query, syncContext).ConfigureAwait(false);
            
            var entities        = new List<JsonEntity>(queryResult.entities.Length);
            foreach (var entity in queryResult.entities) {
                entities.Add(new JsonEntity(entity.Json));
            }
            EntityUtils.GetKeysFromEntities (keyName, entities, syncContext, out _);
            var upsert          = new UpsertEntities { container = container, entities = entities };
            await dstContainer.UpsertEntities(upsert, syncContext).ConfigureAwait(false);
        }
        #endregion
    }
}
