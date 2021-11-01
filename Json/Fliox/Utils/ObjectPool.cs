﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Friflo.Json.Fliox.Utils
{
    public readonly struct Pooled<T> : IDisposable where T : IDisposable
    {
        public  readonly T              instance;
        private readonly ObjectPool<T>  pool;
        
        internal Pooled(ObjectPool<T> pool, T instance) {
            this.instance   = instance;
            this.pool       = pool;
        }

        public void Dispose() {
            pool.Return(instance);
        }
    }
    
    public abstract class ObjectPool<T> : IDisposable where T : IDisposable
    {
        internal abstract T     GetInstance();
        internal abstract void  Return(T instance);
        public   abstract void  Dispose();
        public   abstract int   Usage { get; }
        
        public Pooled<T>        Get() {
            return new Pooled<T>(this, GetInstance());
        }
    }
    
    public sealed class SharedPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ConcurrentStack<T>  stack       = new ConcurrentStack<T>();
        private readonly    ConcurrentStack<T>  instances   = new ConcurrentStack<T>();
        private readonly    Func<T>             factory;
        
        public              int                 Count       => stack.Count;
        public  override    int                 Usage       => 0;
        public  override    string              ToString()  => $"Count: {stack.Count}";

        public SharedPool(Func<T> factory) {
            this.factory = factory;
        }

        public override void Dispose() {
            foreach (var instance in instances) {
                instance.Dispose();
            }
            stack.Clear();
            instances.Clear();
        }
        
        internal override T GetInstance() {
            if (!stack.TryPop(out T instance)) {
                instance = factory();
                instances.Push(instance);
            }
            return instance;
        }
        
        internal override void Return(T instance) {
            if (instance is IResetable resetable) {
                resetable.Reset();
            }
            stack.Push(instance);
        }
    }
    
    public sealed class LocalPool<T> : ObjectPool<T> where T : IDisposable
    {
        private readonly    ObjectPool<T>   pool;
        private             int             count;
        
        public  override    int             Usage       => count;
        public  override    string          ToString()  => pool.ToString();

        public LocalPool(ObjectPool<T> pool) {
            this.pool   = pool;
        }

        public override void Dispose() { }
        
        internal override T GetInstance() {
            count++;
            return pool.GetInstance();
        }
        
        internal override void Return(T instance) {
            count--;
            pool.Return(instance);
        }
    }
}