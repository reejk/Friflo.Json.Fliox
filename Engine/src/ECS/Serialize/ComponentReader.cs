// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Engine.ECS.Utils;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
// ReSharper disable InlineTemporaryVariable
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Friflo.Engine.ECS.Serialize;

/// <summary>
/// Create all components / scripts for an entity from <see cref="JsonValue"/> used as <see cref="DataEntity.components"/>
/// </summary>
internal sealed class ComponentReader
{
    private readonly    ObjectReader                            componentReader;
    private readonly    Dictionary<string, SchemaType>          schemaTypeByKey;
    private readonly    Dictionary<Type,   ScriptType>          scriptTypeByType;
    private readonly    Dictionary<string, TagType>             tagTypeByName;
    private readonly    HashSet<ScriptType>                     scriptTypes;
    private readonly    ArchetypeKey                            searchKey;
    private readonly    Dictionary<BytesHash, RawKey>           rawKeyCache;
    private             Utf8JsonParser                          parser;
    private             Bytes                                   buffer;
    private             RawComponent[]                          components;
    private             int                                     componentCount;
    
    
    internal ComponentReader() {
        buffer                  = new Bytes(128);
        components              = new RawComponent[1];
        componentReader         = new ObjectReader(EntityStoreBase.Static.TypeStore) { ErrorHandler = ObjectReader.NoThrow };
        var schema              = EntityStoreBase.Static.EntitySchema;
        schemaTypeByKey         = schema.schemaTypeByKey;
        scriptTypeByType        = schema.scriptTypeByType;
        tagTypeByName           = schema.tagTypeByName;
        scriptTypes             = new HashSet<ScriptType>(); // Cannot use Script. User code may override Equals() or GetHashCode()
        searchKey               = new ArchetypeKey();
        rawKeyCache             = new Dictionary<BytesHash, RawKey>(BytesHash.Equality);
    }
    
    internal string Read(DataEntity dataEntity, Entity entity, EntityStoreBase store, in ConvertOptions options)
    {
        componentCount      = 0;
        var hasTags         = dataEntity.tags?.Count > 0;
        var hasComponents   = !dataEntity.components.IsNull();
        if (!hasComponents && !hasTags) {
            return null;
        }
        var error = ReadRaw(dataEntity, entity);
        if (error != null) {
            return error;
        }
        SetEntityArchetype(dataEntity, entity, store, options);
        return ReadComponents(entity);
    }
    
    private string ReadRaw (DataEntity dataEntity, Entity entity)
    {
        parser.InitParser(dataEntity.components);
        var ev = parser.NextEvent();
        switch (ev)
        {
            case JsonEvent.Error:
                var error = parser.error.GetMessage();
                return $"{error}. id: {entity.Id}";
            case JsonEvent.ValueNull:
                break;
            case JsonEvent.ObjectStart:
                ev = ReadRawComponents();
                if (ev != JsonEvent.ObjectEnd) {
                    // could support also scalar types in future: string, number or boolean
                    return $"'components' element must be an object. was {ev}. id: {entity.Id}, component: '{parser.key}'";
                }
                break;
            default:
                return $"expect 'components' == object or null. id: {entity.Id}. was: {ev}";
        }
        return null;
    }
    
    private string ReadComponents(Entity entity)
    {
        scriptTypes.Clear();
        foreach (var script in entity.Scripts) {
            var scriptType = scriptTypeByType[script.GetType()];
            scriptTypes.Add(scriptType);
        }
        for (int n = 0; n < componentCount; n++)
        {
            var component = components[n];
            buffer.Clear();
            var json        = new JsonValue(parser.GetInputBytes(component.start - 1, component.end));
            var schemaType  = component.rawKey.schemaType;
            switch (schemaType.Kind) {
                case SchemaTypeKind.Script:
                    // --- read script
                    var scriptType = (ScriptType)schemaType;
                    scriptTypes.Remove(scriptType);
                    scriptType.ReadScript(componentReader, json, entity);
                    break;
                case SchemaTypeKind.Component:
                    var componentType   = (ComponentType)schemaType;
                    var heap            = entity.archetype.heapMap[componentType.StructIndex]; // no range or null check required
                    // --- read & change component
                    heap.Read(componentReader, entity.compIndex, json);
                    break;
            }
            if (componentReader.Error.ErrSet) {
                return $"'components[{component.rawKey.key}]' - {componentReader.Error.GetMessage()}";
            }
        }
        // --- remove missing scripts from entity
        foreach (var scriptType in scriptTypes) {
            EntityUtils.RemoveEntityScript(entity, scriptType);
        }
        return null;
    }
    
    /// <summary>
    /// Ensures the given entity present / moved to an <see cref="Archetype"/> that contains all components 
    /// within the current JSON payload.
    /// </summary>
    private void SetEntityArchetype(DataEntity dataEntity, Entity entity, EntityStoreBase store, in ConvertOptions options)
    {
        var key = searchKey;
        key.Clear();
        var hasComponentTypes   = GetComponentTypes(ref key.componentTypes);
        var tags                = dataEntity.tags;
        var hasTags             = tags?.Count > 0;
        if (!hasComponentTypes && !hasTags) {
            return; // early out in absence of components and tags
        }
        
        if (hasTags) {
            AddTags(tags, key);
        }
        var curType = entity.archetype;
        if (options.preserve) {
            Preserve(key, curType, options);
        }
        key.CalculateHashCode();
        
        // --- use / create Archetype with present components to eliminate structural changes for every individual component Read()
        var newType = FindArchetype(key, store);
        if (curType != newType)
        {
            ref var node    = ref entity.store.nodes[entity.Id];
            node.archetype  = newType;
            node.compIndex  = Archetype.MoveEntityTo(curType, entity.Id, node.compIndex, newType);
        }
    }
    
    private bool GetComponentTypes(ref ComponentTypes componentTypes)
    {
        var hasComponentTypes   = false;
        var count               = componentCount;
        for (int n = 0; n < count; n++)
        {
            ref var component   = ref components[n];
            var schemaType      = component.rawKey.schemaType;
            if (schemaType.Kind == SchemaTypeKind.Component)
            {
                var componentType = (ComponentType)schemaType;
                hasComponentTypes = true;
                componentTypes.bitSet.SetBit(componentType.StructIndex);
            }                
        }
        return hasComponentTypes;
    }
    
    /// <summary> Preserve components and tags present on passed <paramref name="type"/>. </summary>
    private static void Preserve(ArchetypeKey key, Archetype type, in ConvertOptions options)
    {
        key.componentTypes.bitSet.Add(BitSet.Intersect(type.componentTypes.bitSet, options.preserveComponents.bitSet));
        key.tags.          bitSet.Add(BitSet.Intersect(type.tags.          bitSet, options.preserveTags.      bitSet));
    }
    
    private static Archetype FindArchetype(ArchetypeKey searchKey, EntityStoreBase store)
    {
        if (store.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config          = EntityStoreBase.GetArchetypeConfig(store);
        var newArchetype    = Archetype.CreateWithComponentTypes(config, searchKey.componentTypes, searchKey.tags);
        EntityStoreBase.AddArchetype(store, newArchetype);
        return newArchetype;
    }
    
    private JsonEvent ReadRawComponents()
    {
        var ev = parser.NextEvent();
        while (true) {
            switch (ev) {
                case JsonEvent.ObjectStart:
                    var rawKey  = ToRawKey(parser.key);
                    var start   = parser.Position;
                    parser.SkipTree();
                    if (componentCount == components.Length) {
                        ArrayUtils.Resize(ref components, 2 * componentCount);
                    }
                    components[componentCount++] = new RawComponent(rawKey, start, parser.Position);
                    ev = parser.NextEvent();
                    if (ev == JsonEvent.ObjectEnd) {
                        return JsonEvent.ObjectEnd;
                    }
                    break;
                case JsonEvent.ObjectEnd:
                    return JsonEvent.ObjectEnd;
                default:
                    return ev;
            }
        }
    }
    
    private RawKey ToRawKey(in Bytes keyBytes)
    {
        var keyHash = new BytesHash(keyBytes);
        if (rawKeyCache.TryGetValue(keyHash, out RawKey rawKey)) {
            return rawKey;
        }
        var key = keyBytes.AsString();
        if (schemaTypeByKey.TryGetValue(key, out var schemaType)) {
            rawKey  = new RawKey(key, schemaType);
        }
        var bytesCopy = new Bytes(keyBytes);    // must create copy - given key Bytes will be mutated
        rawKeyCache.Add(new BytesHash(bytesCopy), rawKey);
        return rawKey;
    }
    
    private void AddTags(List<string> tagList, ArchetypeKey archetypeKey)
    {
        foreach (var tag in tagList) {
            if (!tagTypeByName.TryGetValue(tag, out var tagType)) {
                continue;
            }
            archetypeKey.tags.bitSet.SetBit(tagType.TagIndex);
        }
    }
}

internal readonly struct RawKey
{
    internal  readonly  string      key;            // never null
    internal  readonly  SchemaType  schemaType;     // never null

    public    override  string      ToString() => $"{key} - {schemaType.Name}";
    
    internal RawKey(string key, SchemaType schemaType) {
        this.key        = key;
        this.schemaType = schemaType;
    }
}

internal readonly struct RawComponent
{
    internal  readonly  RawKey      rawKey;
    internal  readonly  int         start;
    internal  readonly  int         end;

    public    override  string      ToString() => rawKey.ToString();
    
    internal RawComponent(in RawKey rawKey, int start, int end) {
        this.rawKey = rawKey;
        this.start  = start;
        this.end    = end;
    }
}