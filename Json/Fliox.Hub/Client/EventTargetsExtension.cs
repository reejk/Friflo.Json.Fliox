// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Protocol.Models;

namespace Friflo.Json.Fliox.Hub.Client
{
    public static class EventTargetsExtension
    {
        // --- user
        public static  TTask  EventTargetUser<TTask> (this TTask messageTask, string  user) where TTask : MessageTask {
            messageTask.EventTargets.AddClient(new EventTargetClient(user));
            return messageTask;
        }
        
        public static  TTask  EventTargetUser<TTask> (this TTask messageTask, JsonKey user) where TTask : MessageTask{
            messageTask.EventTargets.AddClient(new EventTargetClient(user));
            return messageTask;
        }
        
        // --- user client
        public static  TTask  EventTargetClient<TTask> (this TTask messageTask, string  user, string client) where TTask : MessageTask{
            messageTask.EventTargets.AddClient(new EventTargetClient(user, client));
            return messageTask;
        }
        
        public static  TTask  EventTargetClient<TTask> (this TTask messageTask, JsonKey user, JsonKey client) where TTask : MessageTask{
            messageTask.EventTargets.AddClient(new EventTargetClient(user, client));
            return messageTask;
        }
        
        public static  TTask  EventTargetClient<TTask> (this TTask messageTask, EventTargetClient client) where TTask : MessageTask {
            messageTask.EventTargets.AddClient(client);
            return messageTask;
        }
        
        // --- users
        public static  TTask  EventTargetUsers<TTask> (this TTask messageTask, ICollection<string>  users) where TTask : MessageTask {
            messageTask.EventTargets.AddClients(users);
            return messageTask;
        }
        public static  TTask  EventTargetUsers<TTask> (this TTask messageTask, ICollection<JsonKey>  users) where TTask : MessageTask {
            messageTask.EventTargets.AddClients (users);
            return messageTask;
        }
        
        // --- user clients
        public static  TTask  EventTargetClients<TTask> (this TTask messageTask, ICollection<(string, string)>  clients) where TTask : MessageTask {
            messageTask.EventTargets.AddClients(clients);
            return messageTask;
        }
        public static  TTask  EventTargetClients<TTask> (this TTask messageTask, ICollection<EventTargetClient>  clients) where TTask : MessageTask {
            messageTask.EventTargets.AddClients (clients);
            return messageTask;
        }
    }
}