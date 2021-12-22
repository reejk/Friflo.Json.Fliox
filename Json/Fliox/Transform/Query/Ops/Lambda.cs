﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.Transform.Query.Ops
{
    public sealed class Lambda : Operation
    {
        [Fri.Required]  public          string          arg;
        [Fri.Required]  public          Operation       body;
        
        public   override string    Name => "(o): any";
        public   override void      AppendLinq(AppendCx cx) {
            cx.lambdaArg = arg;
            cx.Append(arg);
            cx.Append(" => ");
            body.AppendLinq(cx);
        }

        public Lambda() { }
        public Lambda(string arg, Operation body) {
            this.arg    = arg;
            this.body   = body;
        }
        
        internal static  void InitBody (Operation body, string arg, OperationContext cx) {
            cx.variables.Add(arg, LambdaArg.Instance);
            body.Init(cx, 0);
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            InitBody(body, arg, cx);
        }
        
        internal override EvalResult Eval(EvalCx cx) {
            var eval = body.Eval(cx);
            return eval;
        }
    }
    
    public sealed class Filter : FilterOperation
    {
        [Fri.Required]  public          string          arg;
        [Fri.Required]  public          FilterOperation body;
        
        public   override string    Name => "(o): bool";
        public   override void      AppendLinq(AppendCx cx) {
            cx.lambdaArg = arg;
            cx.Append(arg);
            cx.Append(" => ");
            body.AppendLinq(cx);
        }

        public Filter() { }
        public Filter(string arg, FilterOperation body) {
            this.arg    = arg;
            this.body   = body;
        }
        
        internal override void Init(OperationContext cx, InitFlags flags) {
            Ops.Lambda.InitBody(body, arg, cx);
        }
        
        internal override EvalResult Eval(EvalCx cx) {
            var eval = body.Eval(cx);
            return eval;
        }
    }
}