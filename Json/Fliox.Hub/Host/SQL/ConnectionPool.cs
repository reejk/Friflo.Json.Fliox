// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public class ConnectionPool<T> where T : ISyncConnection
    {
        private  readonly   ConcurrentStack<T> connectionPool = new ConcurrentStack<T>();
        
        public bool TryPop(out T syncConnection) {
            if (connectionPool.TryPop(out syncConnection) && syncConnection.IsOpen) {
                return true;
            }
            return false;
        }
        
        public void Push(ISyncConnection syncConnection) {
            connectionPool.Push((T)syncConnection);
        }
    }
}