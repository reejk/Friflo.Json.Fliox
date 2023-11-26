﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable ConvertConstructorToMemberInitializers
using System;

namespace Friflo.Fliox.Engine.ECS.Serialize;

public class EntityConverter
{
    private  readonly   ComponentReader reader;
    private  readonly   ComponentWriter writer;
    
    public static readonly EntityConverter Default = new EntityConverter();
    
    public EntityConverter() {
        reader = new ComponentReader();
        writer = new ComponentWriter();
    }
    
    public DataEntity EntityToDataEntity(Entity entity, DataEntity dataEntity = null, bool pretty = false)
    {
        if (entity == null) {
            throw new ArgumentNullException(nameof(entity));
        }
        var store       = entity.archetype.entityStore;
        var pid         = store.GetNodeById(entity.id).pid;
        dataEntity    ??= new DataEntity();
        dataEntity.pid  = pid;
        store.EntityToDataEntity(entity, dataEntity, writer, pretty);
        return dataEntity;
    }
    
    public Entity DataEntityToEntity(DataEntity dataEntity, EntityStore store, out string error)
    {
        if (dataEntity == null) {
            throw new ArgumentNullException(nameof(dataEntity));
        }
        return store.DataEntityToEntity(dataEntity, out error, reader);
    }
}