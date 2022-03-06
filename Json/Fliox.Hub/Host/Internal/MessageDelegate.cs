﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Hub.Host.Internal
{
    internal readonly struct InvokeResult
    {
        internal readonly   JsonValue   value;
        internal readonly   string      error;

        public override     string      ToString() => error ?? value.AsString();

        internal InvokeResult(byte[] value) {
            this.value  = new JsonValue(value);
            this.error  = null;
        }
        
        internal InvokeResult(string error) {
            this.value  = default;
            this.error  = error;
        }
    }
    
    internal enum MsgType {
        Command,
        Message
    }
    
    // ----------------------------------- MessageDelegate -----------------------------------
    internal abstract class MessageDelegate
    {
        // Note! Must not contain any state
        internal  abstract  MsgType             MsgType { get; }  
        
        // return type could be a ValueTask but Unity doesnt support this. 2021-10-25
        internal  abstract  Task<InvokeResult>  InvokeDelegate(string messageName, JsonValue messageValue, ExecuteContext executeContext);
    }
    
    // ----------------------------------- MessageDelegate<> -----------------------------------
    internal sealed class MessageDelegate<TValue> : MessageDelegate
    {
        private  readonly   string                      name;
        private  readonly   HostMessageHandler<TValue>  handler;

        internal override   MsgType                     MsgType     => MsgType.Message;
        public   override   string                      ToString()  => name;

        internal MessageDelegate (string name, HostMessageHandler<TValue> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, ExecuteContext executeContext) {
            var cmd     = new MessageContext(messageName,  executeContext);
            var param   = new Param<TValue> (messageValue, executeContext); 
            handler(param, cmd);
            
            var error = cmd.error;
            if (error != null) {
                return Task.FromResult(new InvokeResult(error));
            }
            return Task.FromResult(new InvokeResult((byte[])null));
        }
    }
    
    // ----------------------------------- CommandDelegate<,> -----------------------------------
    internal sealed class CommandDelegate<TValue, TResult> : MessageDelegate
    {
        private  readonly   string                              name;
        private  readonly   HostCommandHandler<TValue, TResult> handler;

        internal override   MsgType                             MsgType     => MsgType.Command;
        public   override   string                              ToString()  => name;

        internal CommandDelegate (string name, HostCommandHandler<TValue, TResult> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, ExecuteContext executeContext) {
            var cmd     = new MessageContext(messageName,  executeContext);
            var param   = new Param<TValue> (messageValue, executeContext); 
            TResult result  = handler(param, cmd);
            
            var error = cmd.error;
            if (error != null) {
                return Task.FromResult(new InvokeResult(error));
            }
            using (var pooled = executeContext.pool.ObjectMapper.Get()) {
                var writer = pooled.instance.writer;
                writer.WriteNullMembers = cmd.WriteNull;
                writer.Pretty           = cmd.WritePretty;
                var jsonResult          = writer.WriteAsArray(result);
                return Task.FromResult(new InvokeResult(jsonResult));
            }
        }
    }
    
    // ----------------------------------- CommandAsyncDelegate<,> -----------------------------------
    internal sealed class CommandAsyncDelegate<TParam, TResult> : MessageDelegate
    {
        private  readonly   string                                      name;
        private  readonly   HostCommandHandler<TParam, Task<TResult>>   handler;

        internal override   MsgType                                     MsgType     => MsgType.Command;
        public   override   string                                      ToString()  => name;

        internal CommandAsyncDelegate (string name, HostCommandHandler<TParam, Task<TResult>> handler) {
            this.name       = name;
            this.handler    = handler;
        }
        
        internal override async Task<InvokeResult> InvokeDelegate(string messageName, JsonValue messageValue, ExecuteContext executeContext) {
            var cmd     = new MessageContext(messageName,  executeContext);
            var param   = new Param<TParam> (messageValue, executeContext); 
            var result  = await handler(param, cmd).ConfigureAwait(false);
            
            var error   = cmd.error;
            if (error != null) {
                return new InvokeResult(error);
            }
            using (var pooled = executeContext.pool.ObjectMapper.Get()) {
                var writer = pooled.instance;
                writer.WriteNullMembers = cmd.WriteNull;
                writer.Pretty           = cmd.WritePretty;
                var jsonResult          = writer.WriteAsArray(result);
                return new InvokeResult(jsonResult);
            }
        }
    }
}