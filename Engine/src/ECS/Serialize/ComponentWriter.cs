// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Engine.ECS.Utils;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Engine.ECS.Serialize;

/// <summary>
/// Create the <see cref="JsonValue"/> from all components and scripts used at <see cref="DataEntity.components"/>.<br/>
/// </summary>
internal sealed class ComponentWriter
{
    private  readonly   ObjectWriter                    componentWriter;
    private             Utf8JsonWriter                  writer;
    internal            Bytes                           buffer;
    private  readonly   ComponentType[]                 structTypes;
    private  readonly   Dictionary<Type, ScriptType>    scriptTypeByType;
    
    internal ComponentWriter() {
        buffer              = new Bytes(128);
        componentWriter     = new ObjectWriter(EntityStoreBase.Static.TypeStore);
        var schema          = EntityStoreBase.Static.EntitySchema;
        structTypes         = schema.components;
        scriptTypeByType    = schema.scriptTypeByType;
    }
    
    internal JsonValue Write(Entity entity, List<JsonValue> members, bool pretty)
    {
        var archetype = entity.archetype;
        if (entity.ComponentCount() == 0) {
            return default;
        }
        var componentCount = 0;
        writer.InitSerializer();
        writer.SetPretty(pretty);
        writer.ObjectStart();
        // --- write components
        var heaps = archetype.Heaps();
        for (int n = 0; n < heaps.Length; n++) {
            var heap = heaps[n];
            var componentType = structTypes[heap.structIndex];
            if (componentType.ComponentKey == null) {
                continue;
            }
            var value           = heap.Write(componentWriter, entity.compIndex);
            var keyBytes        = componentType.componentKeyBytes;
            var start           = writer.json.end;
            writer.MemberBytes(keyBytes.AsSpan(), value);
            members?.AddMember(writer, start);
            componentCount++;
        }
        // --- write scripts
        foreach (var script in entity.Scripts) {
            componentWriter.WriteObject(script, ref buffer);
            var classType   = scriptTypeByType[script.GetType()];
            var keyBytes    = classType.componentKeyBytes;
            var start       = writer.json.end;
            writer.MemberBytes(keyBytes.AsSpan(), buffer);
            members?.AddMember(writer, start);
            componentCount++;
        }
        if (componentCount == 0) {
            return default;
        }
        writer.ObjectEnd();
        return new JsonValue(writer.json);
    }
}

internal static class ComponentWriterExtensions
{
    internal static void AddMember(this List<JsonValue> members, Utf8JsonWriter writer, int start)
    {
        var buffer = writer.json.buffer;
        if (buffer[start] == ',') {
            start++;
        }
        members.Add(new JsonValue(buffer, start, writer.json.end - start));
    }
}