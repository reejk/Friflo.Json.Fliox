﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper.Map;
using static System.Diagnostics.DebuggerBrowsableState;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable UseDeconstruction
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// The main use case is assigning a single <see cref="DatabaseService"/> to an <see cref="EntityDatabase"/> to declare
    /// custom command handler methods annotated with <c>[CommandHandler]</c>. E.g.<br/>
    /// <code>
    ///     [CommandHandler]
    ///     async Task&lt;Result&lt;TResult&gt;&gt; MyCommand(Param&lt;TParam&gt; param, MessageContext context)
    /// </code> 
    /// </summary>
    /// 
    /// <remarks>
    /// Additional to commands a <see cref="DatabaseService"/> can be used to declare message handler methods. E.g.<br/>
    /// <code>
    ///     [MessageHandler]
    ///     void MyMessage(Param&lt;TParam&gt; param, MessageContext context) { }
    /// </code>
    /// <br/>
    /// <i>Note</i>: Message handler methods - in contrast to command handlers - doesn't return a result.<br/>
    /// <br/>
    /// A <see cref="DatabaseService"/> can also be used to intercept / customize execution of all commands or
    /// database operations by overriding <see cref="ExecuteTask"/> or <see cref="ExecuteTaskAsync"/>  
    /// </remarks>
    public partial class DatabaseService
    {
        [DebuggerBrowsable(Never)]
        private readonly    Dictionary<ShortString, MessageDelegate>    handlers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             IReadOnlyCollection<MessageDelegate>        Handlers    => handlers.Values;
        public  readonly    DatabaseServiceQueue                        queue;


        /// <summary>
        /// If <paramref name="queue"/> is set <see cref="SyncRequest"/> are queued for execution otherwise
        /// they are executed as they arrive.
        /// </summary>
        /// <remarks>
        /// To execute queued requests (<paramref name="queue"/> is set) <see cref="DatabaseServiceQueue.ExecuteQueuedRequestsAsync()"/>
        /// need to be called regularly.<br/>
        /// This enables requests / task execution on the calling thread. <br/>
        /// This mode guarantee sequential execution of messages, commands and container operations like
        /// read, query, create, upsert, merge and delete.<br/>
        /// So using lock's or other thread synchronization mechanisms are not necessary.
        /// </remarks> 
        public DatabaseService (DatabaseServiceQueue queue = null) {
            handlers = new Dictionary<ShortString, MessageDelegate>(ShortString.Equality);
            if (!AddAttributedHandlers(out var error)) {
                throw new InvalidOperationException(error);
            }
            AddStdCommandHandlers();
            this.queue = queue;
        }
        
        protected internal virtual ExecutionType GetExecutionType(SyncRequest syncRequest) {
            return queue != null ? ExecutionType.Queue : syncRequest.intern.executionType;
        }
        
        protected internal virtual void PreExecuteTasks (SyncContext syncContext)  { }
        protected internal virtual void PostExecuteTasks(SyncContext syncContext)  { }
        
        protected internal virtual void CustomizeCreate (CreateEntities task, SyncContext syncContext) { }
        protected internal virtual void CustomizeUpsert (UpsertEntities task, SyncContext syncContext) { }
        protected internal virtual void CustomizeMerge  (MergeEntities  task, SyncContext syncContext) { }
        protected internal virtual void CustomizeDelete (DeleteEntities task, SyncContext syncContext) { }
        
        public virtual async Task<SyncTaskResult> ExecuteTaskAsync (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (!AuthorizeTask(task, syncContext, out var error))
                return error;
            return await task.ExecuteAsync(database, response, syncContext).ConfigureAwait(false);
        }
        
        public virtual            SyncTaskResult  ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (!AuthorizeTask(task, syncContext, out var error))
                return error;
            return task.Execute(database, response, syncContext);
        }
        
        protected static bool AuthorizeTask(SyncRequestTask task, SyncContext syncContext, out SyncTaskResult error) {
            var taskAuthorizer = syncContext.authState.taskAuthorizer;
            if (taskAuthorizer.AuthorizeTask(task, syncContext)) {
                error = null;
                return true;
            }
            var sb = new StringBuilder(); // todo StringBuilder could be pooled
            sb.Append("not authorized");
            var authError = syncContext.authState.error; 
            if (authError != null) {
                sb.Append(". ");
                sb.Append(authError);
            }
            var request = syncContext.request;
            if (!request.userId.IsNull()) {
                sb.Append(". user: ");
                sb.Append(request.userId);
            }
            var message = sb.ToString();
            error = SyncRequestTask.PermissionDenied(message);
            return false;
        }
        
        // ------------------- API to add instance / static and synchronous / async methods -------------------
        /// <summary>
        /// Add a synchronous message handler method with a method signature like:
        /// <code>
        /// void TestMessage(Param&lt;string&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandler<TParam> (string name, HostMessageHandler<TParam> handler) {
            var message = new MessageDelegate<TParam>(name, handler);
            handlers.Add(new ShortString(name), message);
        }
        
        /// <summary>
        /// Add an asynchronous message handler method with a method signature like:
        /// <code>
        /// Task TestMessage(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// message handler methods can be static or instance methods.
        /// </summary>
        protected void AddMessageHandlerAsync<TParam> (string name, HostMessageHandlerAsync<TParam> handler) {
            var message = new MessageDelegateAsync<TParam>(name, handler);
            handlers.Add(new ShortString(name), message);
        }
        
        /// <summary>
        /// Add a synchronous command handler method with a method signature like:
        /// <code>
        /// bool TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandHandler<TParam, TResult> (string name, HostCommandHandler<TParam, TResult> handler) {
            var command = new CommandDelegate<TParam, TResult>(name, handler);
            handlers.Add(new ShortString(name), command);
        }

        /// <summary>
        /// Add an asynchronous command handler method with a method signature like:
        /// <code>
        /// Task&lt;bool&gt; TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// command handler methods can be static or instance methods.
        /// </summary>
        protected void AddCommandHandlerAsync<TParam, TResult> (string name, HostCommandHandlerAsync<TParam, TResult> handler) {
            var command = new CommandDelegateAsync<TParam, TResult>(name, handler);
            handlers.Add(new ShortString(name), command);
        }
       
        /// <summary>
        /// Add all methods of the given class <paramref name="instance"/> with the parameters <br/>
        /// (<see cref="Param{TParam}"/> param, <see cref="MessageContext"/> context) as a message/command handler. <br/>
        /// A command handler has return type - a message handler returns void. <br/>
        /// Command handler example:
        /// <code>
        /// bool TestCommand(Param&lt;TestCommand&gt; param, MessageContext context) { ... }
        /// </code>
        /// Message handler methods can be: <br/>
        /// - static or instance methods <br/>
        /// - synchronous or asynchronous - using <see cref="Task{TResult}"/> as return type.
        /// </summary>
        /// <param name="instance">the instance of class containing message handler methods.
        ///     Commonly the instance of a <see cref="DatabaseService"/></param>
        /// <param name="messagePrefix">the prefix of a message/command - e.g. "test."; null or "" to add messages without prefix</param>
        [Obsolete("use attributed command / message handler instead: [CommandHandler] or [MessageHandler]", false)]
        protected void AddMessageHandlers<TClass>(TClass instance, string messagePrefix) where TClass : class
        {
            var type        = typeof(TClass);
            var serviceInfo = DatabaseServiceUtils.GetHandlers(type);
            if (serviceInfo == null) {
                return;
            }
            foreach (var handler in serviceInfo.handlers) {
                MessageDelegate messageDelegate;
                if (handler.resultType == typeof(void)) {
                    messageDelegate = CreateMessageCallback(instance, handler, messagePrefix);
                } else {
                    messageDelegate = CreateCommandCallback(instance, handler, messagePrefix);
                }
                handlers.Add(new ShortString(messageDelegate.name), messageDelegate);
            }
        }
        
        private bool AddAttributedHandlers(out string error) {
            var type        = GetType();
            var serviceInfo = DatabaseServiceUtils.GetAttributedHandlers(type);
            if (serviceInfo == null) {
                error = null;
                return true;
            }
            if (serviceInfo.error != null) {
                error = serviceInfo.error;
                return false;
            }
            foreach (var handler in serviceInfo.handlers) {
                MessageDelegate messageDelegate;
                if (handler.resultType == typeof(void)) {
                    messageDelegate = CreateMessageCallback(this, handler, null);
                } else {
                    messageDelegate = CreateCommandCallback(this, handler, null);
                }
                handlers.Add(new ShortString(messageDelegate.name), messageDelegate);
            }
            error = null;
            return true;
        }
        
        private static string GetHandlerName(HandlerInfo handler, string messagePrefix) {
            if (string.IsNullOrEmpty(messagePrefix))
                return handler.name;
            return $"{messagePrefix}{handler.name}";
        }
        
        private static MessageDelegate  CreateMessageCallback<TClass>(
            TClass      handlerClass,
            HandlerInfo handler,
            string      messagePrefix) where TClass : class
        {
            var genericArgs         = new Type[1];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            Type genericTypeArgs;
            if (handler.isAsync) {
                genericTypeArgs     = typeof(HostMessageHandlerAsync<>).MakeGenericType(genericArgs);
            } else {
                genericTypeArgs     = typeof(HostMessageHandler<>).MakeGenericType(genericArgs);    
            }
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = GetHandlerName(handler, messagePrefix);
            constructorParams[1]    = handlerDelegate;
            object instance;
            if (handler.isAsync) {
                instance = TypeMapperUtils.CreateGenericInstance(typeof(MessageDelegateAsync<>), genericArgs, constructorParams);
            } else {
                instance = TypeMapperUtils.CreateGenericInstance(typeof(MessageDelegate<>),      genericArgs, constructorParams);
            }
            return (MessageDelegate)instance;
        }
        
        private static MessageDelegate  CreateCommandCallback<TClass>(
            TClass      handlerClass,
            HandlerInfo handler,
            string      messagePrefix) where TClass : class
        {
            var genericArgs         = new Type[2];
            var constructorParams   = new object[2];
            // if (handler.name == "DbContainers") { int i = 1; }
            genericArgs[0]          = handler.valueType;
            genericArgs[1]          = handler.resultType;
            Type genericTypeArgs;
            if (handler.isAsync) {
                genericTypeArgs     = typeof(HostCommandHandlerAsync<,>).MakeGenericType(genericArgs);
            } else {
                genericTypeArgs     = typeof(HostCommandHandler<,>).MakeGenericType(genericArgs);
            }
            var firstArgument       = handler.method.IsStatic ? null : handlerClass;
            var handlerDelegate     = Delegate.CreateDelegate(genericTypeArgs, firstArgument, handler.method);

            constructorParams[0]    = GetHandlerName(handler, messagePrefix);
            constructorParams[1]    = handlerDelegate;
            object instance;
            // is return type of command handler of type: Task<TResult> ?  (==  is async command handler)
            if (handler.isAsync) {
                instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandDelegateAsync<,>), genericArgs, constructorParams);
            } else {
                instance = TypeMapperUtils.CreateGenericInstance(typeof(CommandDelegate<,>),      genericArgs, constructorParams);    
            }
            return (MessageDelegate)instance;
        }
        
        // --- internal API ---
        internal bool TryGetMessage(in ShortString name, out MessageDelegate message) {
            return handlers.TryGetValue(name, out message);
        }
        
        internal string[] GetMessages() {
            var count   = CountMessageTypes(MsgType.Message);
            var result  = new string[count];
            int         n = 0;
            foreach (var pair in handlers) {
                if (pair.Value.MsgType == MsgType.Message)
                    result[n++] = pair.Key.AsString();
            }
            return result;
        }
        
        internal string[] GetCommands() {
            var count   = CountMessageTypes(MsgType.Command);
            var result  = new string[count];
            int n       = 0;
            // add std. commands on the bottom
            AddCommands(result, ref n, false, handlers);
            AddCommands(result, ref n, true,  handlers);
            return result;
        }
        
        private int CountMessageTypes (MsgType msgType) {
            int count = 0;
            foreach (var pair in handlers) {
                if (pair.Value.MsgType == msgType) count++;
            }
            return count;
        }
        
        private static void AddCommands (string[] commands, ref int n, bool standard, Dictionary<ShortString, MessageDelegate> commandMap) {
            foreach (var pair in commandMap) {
                if (pair.Value.MsgType != MsgType.Command)
                    continue;
                var name = pair.Key.ToString();
                if (name.StartsWith("std.") == standard)
                    commands[n++] = name;
            }
        }
    }
}