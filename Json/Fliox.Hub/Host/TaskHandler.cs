﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host
{
    public delegate TResult CommandHandler<TValue, out TResult>(Command<TValue> command);

    /// <summary>
    /// A <see cref="TaskHandler"/> is attached to every <see cref="EntityDatabase"/> to handle all
    /// <see cref="SyncRequest.tasks"/> of a <see cref="SyncRequest"/>.
    /// <br/>
    /// Each task is either a database operation or a custom command.
    /// <list type="bullet">
    ///   <item>
    ///     <b>Database operations</b> are a build-in functionality of every <see cref="EntityDatabase"/>.
    ///     These operations are:
    ///     <see cref="CreateEntities"/>, <see cref="UpsertEntities"/>, <see cref="DeleteEntities"/>,
    ///     <see cref="PatchEntities"/>, <see cref="ReadEntities"/> or <see cref="QueryEntities"/>.
    ///   </item>
    ///   <item>
    ///     <b>Custom commands</b> are added by an application to perform custom operations.
    ///     Each command is a tuple of its name and its command value. See <see cref="SendCommand"/>.
    ///     When executed by its handler method it returns a command result. See <see cref="SendCommandResult"/>. 
    ///   </item>
    /// </list>  
    /// </summary>
    public class TaskHandler
    {
        private readonly Dictionary<string, CommandCallback> commands = new Dictionary<string, CommandCallback>();
        
        public TaskHandler () {
            AddCommandHandler(StdCommand.Echo, new CommandHandler<JsonValue, JsonValue>(Echo));
        }
        
        private static JsonValue Echo (Command<JsonValue> command) {
            return command.JsonValue;
        }
        
        internal bool TryGetCommand(string name, out CommandCallback command) {
            return commands.TryGetValue(name, out command); 
        }
        
        public void AddCommandHandler<TValue, TResult>(string name, CommandHandler<TValue, TResult> handler) {
            var command = new CommandCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }
        
        public void AddCommandHandlerAsync<TValue, TResult>(string name, CommandHandler<TValue, Task<TResult>> handler) {
            var command = new CommandAsyncCallback<TValue, TResult>(name, handler);
            commands.Add(name, command);
        }

        private static bool AuthorizeTask(SyncRequestTask task, MessageContext messageContext, out SyncTaskResult error) {
            var authorizer = messageContext.authState.authorizer;
            if (authorizer.Authorize(task, messageContext)) {
                error = null;
                return true;
            }
            var message = "not authorized";
            var authError = messageContext.authState.error; 
            if (authError != null) {
                message = $"{message}. {authError}";
            }
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        public virtual async Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (AuthorizeTask(task, messageContext, out var error)) {
                var result = await task.Execute(database, response, messageContext).ConfigureAwait(false);
                return result;
            }
            return error;
        }
    }
}