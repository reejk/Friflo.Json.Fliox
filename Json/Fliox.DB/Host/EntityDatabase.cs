// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol;

namespace Friflo.Json.Fliox.DB.Host
{
    public class DbOpt {
        /// <see cref="EntityDatabase.customContainerName"/>
        public  readonly    CustomContainerName customContainerName;
        
        public DbOpt(CustomContainerName customContainerName = null) {
            this.customContainerName    = customContainerName   ?? (name => name);
        }
        
        internal static readonly DbOpt Default = new DbOpt();
    }
    
    public delegate string CustomContainerName(string name);
    
    /// <summary>
    /// <see cref="EntityDatabase"/> is the abstraction for specific database adapters / implementations e.g. a
    /// <see cref="MemoryDatabase"/> or <see cref="FileDatabase"/>.
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
    /// </summary>
    public abstract class EntityDatabase : IDisposable
    {
        /// <summary> map of of containers identified by their container name </summary>
        private readonly    Dictionary<string, EntityContainer> containers = new Dictionary<string, EntityContainer>();
        
        /// <summary>
        /// An optional <see cref="DatabaseSchema"/> used to validate the JSON payloads in all write operations
        /// performed on the <see cref="EntityContainer"/>'s of the database
        /// </summary>
        public              DatabaseSchema      Schema          { get; set; }
        
        /// <summary>
        /// A mapping function used to assign a custom container name.
        /// If using a custom name its value is assigned to the containers <see cref="EntityContainer.instanceName"/>. 
        /// By having the mapping function in <see cref="EntityContainer"/> it enables uniform mapping across different
        /// <see cref="EntityContainer"/> implementations.
        /// </summary>
        public readonly     CustomContainerName customContainerName;
        
        /// <summary>
        /// The <see cref="taskHandler"/> execute all <see cref="SyncRequest.tasks"/> send by a client.
        /// An <see cref="EntityDatabase"/> implementation is able to assign as custom handler by constructor
        /// </summary>
        public   readonly   TaskHandler         taskHandler;

        internal readonly   string              name;
        internal readonly   FlioxHub            hub;

        public override     string              ToString() => name != null ? $"'{name}'" : "";

        protected EntityDatabase(TaskHandler taskHandler, DbOpt opt){
            customContainerName = (opt ?? DbOpt.Default).customContainerName;
            this.taskHandler = taskHandler ?? new TaskHandler();
        }
        
        protected EntityDatabase (
            FlioxHub    hub,
            string      name,
            TaskHandler taskHandler,
            DbOpt       opt = null
            ) : this(taskHandler, opt)
        {
            this.hub    = hub  ?? throw new ArgumentNullException(nameof(hub));
            this.name   = name ?? throw new ArgumentNullException(nameof(name));
        }

        public virtual void Dispose() {
            foreach (var container in containers ) {
                container.Value.Dispose();
            }
        }
        
        public virtual Task ExecuteSyncPrepare (SyncRequest syncRequest, MessageContext messageContext) {
            return Task.CompletedTask;
        }

        internal void AddContainer(EntityContainer container) {
            containers.Add(container.name, container);
        }
        
        protected bool TryGetContainer(string name, out EntityContainer container) {
            return containers.TryGetValue(name, out container);
        }

        public EntityContainer GetOrCreateContainer(string name)
        {
            if (containers.TryGetValue(name, out EntityContainer container))
                return container;
            containers[name] = container = CreateContainer(name, this);
            return container;
        }
        
        public abstract EntityContainer CreateContainer     (string name, EntityDatabase database);

    }
}