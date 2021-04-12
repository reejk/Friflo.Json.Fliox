﻿using Friflo.Json.Flow.Mapper.Utils;

namespace Friflo.Json.Flow.Mapper.Map
{
    public interface ITracerContext
    {
        
    }
    
    public class Tracer
    {
        public readonly     TypeCache       typeCache;
        public readonly     ITracerContext  tracerContext;

        public Tracer(TypeCache typeCache, ITracerContext tracerContext) {
            this.typeCache = typeCache;
            this.tracerContext = tracerContext;
        }
        
        public void Trace<T>(T value) {
            var mapper = (TypeMapper<T>)typeCache.GetTypeMapper(typeof(T));
            if (!mapper.IsNull(ref value))
                mapper.Trace(this, value);
        }
    }
}