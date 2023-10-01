﻿// ReSharper disable once CheckNamespace
namespace Fliox.Engine.ECS;

internal struct StandardComponents
{
    internal    StructHeap<Position>    position;
    internal    StructHeap<Rotation>    rotation;
    internal    StructHeap<Scale3>      scale3;
    internal    StructHeap<EntityName>  name;
}

internal struct ArchetypeConfig
{
    internal    EntityStore store;
    internal    int         archetypeIndex;
    internal    int         archetypeMax;
    internal    int         capacity;
}