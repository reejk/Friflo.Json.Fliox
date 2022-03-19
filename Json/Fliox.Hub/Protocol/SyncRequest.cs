﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Protocol
{
    // ----------------------------------- request -----------------------------------
    public sealed class SyncRequest : ProtocolRequest
    {
        /// <summary>
        /// Identify the user performing a sync request.
        /// In case using of using <see cref="DB.UserAuth.UserAuthenticator"/> the <see cref="userId"/> and <see cref="token"/>
        /// are use for user authentication.
        /// </summary>
        [Fri.Property(Name = "user")]   public  JsonKey                 userId;
                                        public  string                  token;
        /// <summary>
        /// <see cref="eventAck"/> is used to ensure (change) events are delivered reliable.
        /// A client set <see cref="eventAck"/> to the last received <see cref="ProtocolEvent.seq"/> in case
        /// it has subscribed to database changes by a <see cref="SubscribeChanges"/> task.
        /// Otherwise <see cref="eventAck"/> is null.
        /// </summary>
        [Fri.Property(Name = "ack")]    public  int?                    eventAck;
        /// <summary>list of container operations and database commands / messages</summary>
        [Fri.Required]                  public  List<SyncRequestTask>   tasks;
        /// <summary>database name the <see cref="tasks"/> apply to. null to access the default database</summary>
                                        public  string                  database;
        /// <summary>optional JSON value - can be used to describe a request</summary>
                                        public  JsonValue               info;
        
        internal override                       MessageType             MessageType => MessageType.sync;
    }
}
