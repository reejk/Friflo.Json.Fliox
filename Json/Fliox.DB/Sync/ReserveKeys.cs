// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    public class ReserveKeys  : DatabaseTask {
        [Fri.Required]  public  string          container;
        [Fri.Required]  public  int             count;

        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var store           = new SequenceStore(database, SyncTypeStore.Get(), null);
            var read            = store.sequence.Read();
            var sequenceTask    = read.Find(container);
            await store.Sync();
            var sequence = sequenceTask.Result;
            if (sequence == null) {
                sequence = new _Sequence {
                    container   = container,
                    autoId      = count 
                };
            } else {
                sequence.autoId += count;
            }
            var reservedKeys = new _ReservedKeys {
                token       = Guid.NewGuid(),
                container   = container,
                start       = sequence.autoId,
                count       = count,
                user        = messageContext.clientId
            };
            store.reservedKeys.Upsert(reservedKeys);
            store.sequence.Upsert(sequence);
            var sync = await store.TrySync();
            if (!sync.Success) {
                return  new ReserveKeysResult { Error = new CommandError{message = sync.Message} };
            }
            var result = new ReserveKeysResult {
                start = sequence.autoId,
                count = count,
                token = reservedKeys.token
            };
            return result;
        }

        internal override       TaskType        TaskType => TaskType.reserveKeys;
        public   override       string          TaskName => $"container: '{container}'";
    }
    
    public class ReserveKeysResult : TaskResult {
        [Fri.Required]  public  long            start;
        [Fri.Required]  public  int             count;
        [Fri.Required]  public  Guid            token;
        
                        public  CommandError    Error { get; set; }
        internal override       TaskType        TaskType => TaskType.reserveKeys;
    }
}