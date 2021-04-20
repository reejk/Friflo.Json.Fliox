﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    // Change to attribute
    public class Entity {
        public string   id;
    }
    
    public class Ref<T>  where T : Entity
    {
        internal    PeerEntity<T>   peer;
        private     T               entity;
        private     string          id;

        // either id or entity is set. Never both
        public string   Id {
            get => entity != null ? entity.id : id;
            set { id = value; entity = null; }
        }

        public T GetEntity() {
            return entity;
        }

        public T        Entity {
            get {
                if (entity != null)
                    return entity;
                if (peer != null) {
                    if (peer.assigned)
                        return peer.entity;
                    throw new PeerNotAssignedException(peer.entity);
                }
                return null;
            }
            set { entity = value; id = null; }
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            Ref<T> other = (Ref<T>)obj;
            return Id.Equals(other.Id);
        }

        public override int GetHashCode() {
            return Id.GetHashCode();
        }

        public static implicit operator Ref<T>(T entity) {
            var reference = new Ref<T>();
            reference.entity    = entity;
            return reference;
        }
        
        /* public static implicit operator T(Ref<T> reference) {
            return reference.entity;
        } */


        public static implicit operator Ref<T>(string id) {
            var reference = new Ref<T>();
            reference.id    = id;
            return reference;
        }
    }
}