// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedReadonlyField
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
namespace Friflo.Json.Fliox.DB.Host.NodeInfo
{
    public partial class  NodeInfoStore :  EntityStore
    {
        public  readonly   EntitySet <JsonKey, ClientInfo>  clients;
        public  readonly   EntitySet <JsonKey, UserInfo>    users;
        
        public NodeInfoStore(EntityDatabase database, TypeStore typeStore)
            : base(database, typeStore, null, null) { }
    }
    
    public class ClientInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  Ref<JsonKey, UserInfo>          user;
                        public  int                             requests;
                        public  int                             tasks;
        [Fri.Property (Name =                                  "event")]  
                        public  EventInfo?                      ev;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
    
    public struct EventInfo {
                        public  int                             seq;
                        public  int                             queued;
                        public  List<string>                    messageSubs;
                        public  List<SubscribeChanges>          changeSubs;
    }
    
    public class UserInfo {
        [Fri.Required]  public  JsonKey                         id;
        [Fri.Required]  public  List<Ref<JsonKey, ClientInfo>>  clients;
                        public  int                             requests;
                        public  int                             tasks;
                        
        public override         string ToString() => JsonDebug.ToJson(this, false);
    }
}
