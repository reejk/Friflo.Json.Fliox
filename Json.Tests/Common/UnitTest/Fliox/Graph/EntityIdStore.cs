﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Database;
using Friflo.Json.Fliox.Graph;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UnassignedReadonlyField
namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Graph
{
    public class EntityIdStore : EntityStore {
        public  readonly    EntitySet <Guid,    GuidEntity>      guidEntities;
        public  readonly    EntitySet <int,     IntEntity>       intEntities;
        public  readonly    EntitySet <long,    LongEntity>      longEntities;
        public  readonly    EntitySet <short,   ShortEntity>     shortEntities;
        public  readonly    EntitySet <byte,    ByteEntity>      byteEntities;
        public  readonly    EntitySet <string,  CustomIdEntity>  customIdEntities;
        public  readonly    EntitySet <string,  EntityRefs>      entityRefs;
        public  readonly    EntitySet <string,  CustomIdEntity2> customIdEntities2;

        public EntityIdStore(EntityDatabase database, TypeStore typeStore, string clientId) : base(database, typeStore, clientId) {}
    }

    public class GuidEntity {
        public Guid id;
    }
    
    public class IntEntity {
        public int  id;
    }
    
    public class LongEntity {
        public long Id { get; set; }
    }
    
    public class ShortEntity {
        public short id;
    }
    
    public class ByteEntity {
        public byte id;
    }
    
    public class CustomIdEntity {
        [Fri.Key]
        [Fri.Required]  public string customId;
    }
    
    public class EntityRefs {
        [Fri.Required]  public string                       id;
                        public Ref <Guid,   GuidEntity>     guidEntity;
                        public Ref <int,    IntEntity>      intEntity;
                        public Ref <long,   LongEntity>     longEntity;
                        public Ref <short,  ShortEntity>    shortEntity;
                        public Ref <byte,   ByteEntity>     byteEntity;
                        public Ref <string, CustomIdEntity> customIdEntity;
                        public List<Ref <int, IntEntity>>   intEntities;
    }

    public class CustomIdEntity2 {
#if UNITY_5_3_OR_NEWER
        [Fri.Key] [Fri.Required]
#else
        // Apply [Key]      alternatively by System.ComponentModel.DataAnnotations.KeyAttribute
        // Apply [Required] alternatively by System.ComponentModel.DataAnnotations.RequiredAttribute
        [Key] [Required]
#endif
        public string customId2;
    }

}