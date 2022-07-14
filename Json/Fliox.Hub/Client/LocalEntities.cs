// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Friflo.Json.Fliox.Hub.Client.Internal;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Provide access to entities tracked by an <see cref="EntitySet{TKey,T}"/>. <br/>
    /// An entity become tracked if the <see cref="EntitySet{TKey,T}"/> gets aware of an entity by following calls  
    /// <list type="bullet">
    ///   <item>.Create(), .CreateRange(), .Upsert(), .UpsertRange()</item>
    ///   <item>.Read().Find() or .FindRange()</item>
    ///   <item>.Query(), .QueryAll(), .QueryByFilter()</item>
    /// </list> 
    /// </summary>
    public readonly struct LocalEntities<TKey, T> where T : class
    {
        [DebuggerBrowsable(Never)]
        private  readonly   EntitySet<TKey, T>  entitySet;
        // ReSharper disable once UnusedMember.Local - expose entities as list in Debugger
        private             IEnumerable<T>      Entities => entitySet.PeerMap().Values.Select(peer => peer.Entity);

        public LocalEntities (EntitySet<TKey, T> entitySet) {
            this.entitySet  = entitySet;
        }
    
        /// <summary>
        /// Get the <paramref name="entity"/> with the passed <paramref name="key"/> from the <see cref="EntitySet"/>. <br/>
        /// Return true if the <see cref="EntitySet{TKey,T}"/> contains an entity with the given key. Otherwise false.
        /// </summary>
        public bool TryGet (TKey key, out T entity) {
            var peers = entitySet.PeerMap();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                entity = peer.NullableEntity;
                return true;
            }
            entity = null;
            return false;
        }
        
        /// <summary>
        /// Return true if the <see cref="EntitySet{TKey,T}"/> contains an entity with the passed <paramref name="key"/>
        /// </summary>
        public bool Contains (TKey key) {
            var peers = entitySet.PeerMap();
            return peers.ContainsKey(key);
        }
        
        /// <summary>
        /// Return all tracked entities of the <see cref="EntitySet{TKey,T}"/>
        /// </summary>
        public List<T> ToList() {
            var peers   = entitySet.PeerMap();
            var result  = new List<T>(peers.Count);
            foreach (var pair in peers) {
                var entity = pair.Value.NullableEntity;
                if (entity == null)
                    continue;
                result.Add(entity);
            }
            return result;
        }
    }
}