﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Fliox.Engine.ECS.Serialize;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

internal class JsonConvert
{
    private readonly    EntityConverter converter;
    private readonly    DataEntity      dataEntity;      
    private readonly    ObjectWriter    objectWriter;
    private             Utf8JsonParser  parser;
    private             Utf8JsonWriter  jsonWriter;
    
    internal JsonConvert()
    {
        converter       = new EntityConverter();
        dataEntity      = new DataEntity();      
        objectWriter    = new (new TypeStore()) {  // todo use global TypeStore
            Pretty              = true,
            WriteNullMembers    = false
        };
    }
    
    internal string EntityToJSON(Entity entity)
    {
        converter.EntityToDataEntity(entity, dataEntity, true);
        return objectWriter.Write(dataEntity);
    }
    
    internal string DataEntityToJSON(DataEntity data)
    {
        parser.InitParser(data.components);
        var error = Traverse();
        if (error != null) {
            return error;
        }
        dataEntity.pid          = data.pid;
        dataEntity.tags         = data.tags;
        dataEntity.children     = data.children;
        dataEntity.components   = new JsonValue(jsonWriter.json);
        
        return objectWriter.Write(dataEntity);
    }
    
    /// <summary> used to format the <see cref="DataEntity.components"/> - one component per line</summary>
    private string Traverse()
    {
        var ev = parser.NextEvent();
        switch (ev)
        {
            case JsonEvent.Error:
                return parser.error.GetMessage();
            case JsonEvent.ValueNull:
                break;
            case JsonEvent.ObjectStart:
                jsonWriter.InitSerializer();
                ev = TraverseComponents();
                if (ev != JsonEvent.ObjectEnd) {
                    return $"component must be an object. was {ev}, component: '{parser.key}'";
                }
                break;
            default:
                return $"expect 'components' == object or null. was: {ev}";
        }
        return null;
    }
    
    private static readonly Bytes   ComponentsStart = new Bytes("{\n");
    private static readonly Bytes   KeyStart        = new Bytes("        \"");
    private static readonly Bytes   KeyEnd          = new Bytes("\": ");
    private static readonly Bytes   ComponentNext   = new Bytes(",\n");
    private static readonly Bytes   ComponentsEnd   = new Bytes("\n    }");
    
    private JsonEvent TraverseComponents()
    {
        ref var json = ref jsonWriter.json;
        json.AppendBytes(ComponentsStart);
        var ev = parser.NextEvent();
        while (true)
        {
            switch (ev)
            {
                case JsonEvent.ObjectStart:
                    json.AppendBytes(KeyStart);
                    json.AppendBytes(parser.key);
                    json.AppendBytes(KeyEnd);
                    jsonWriter.WriteTree(ref parser);
                    ev = parser.NextEvent();
                    if (ev == JsonEvent.ObjectEnd) {
                        json.AppendBytes(ComponentsEnd);
                        return JsonEvent.ObjectEnd;
                    }
                    json.AppendBytes(ComponentNext);
                    break;
                default:
                    return ev;
            }
        }
    }
}