// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
using System.Collections.Generic;
using Friflo.Json.Flow.Mapper;
using System;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace EntityIdStore2.Graph {

public class GuidEntity {
    Guid  id;
}

public class IntEntity {
    int  id;
}

public class LongEntity {
    long  Id;
}

public class ShortEntity {
    short  id;
}

public class CustomIdEntity {
    [Fri.Key]
    [Fri.Required]
    string  customId;
}

public class EntityRefs {
    [Fri.Required]
    string        id;
    string        guidEntity;
    string        intEntity;
    string        longEntity;
    string        shortEntity;
    string        customIdEntity;
    List<string>  guidEntities;
}

public class CustomIdEntity2 {
    [Fri.Key]
    [Fri.Required]
    string  customId2;
}

}

