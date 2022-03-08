// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { Guid }         from "./Standard";
import { int32 }        from "./Standard";
import { int64 }        from "./Standard";
import { int16 }        from "./Standard";
import { uint8 }        from "./Standard";

// schema documentation only - not implemented right now
export interface EntityIdStore {
    // --- containers
    guidEntities       : { [key: string]: GuidEntity };
    intEntities        : { [key: string]: IntEntity };
    intEntitiesAuto    : { [key: string]: AutoIntEntity };
    longEntities       : { [key: string]: LongEntity };
    shortEntities      : { [key: string]: ShortEntity };
    byteEntities       : { [key: string]: ByteEntity };
    customIdEntities   : { [key: string]: CustomIdEntity };
    entityRefs         : { [key: string]: EntityRefs };
    customIdEntities2  : { [key: string]: CustomIdEntity2 };

    // --- commands
    /** echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]           (param: any) : any;
    /** list all containers of the database */
    ["std.Containers"]     () : DbContainers;
    /** list all commands exposed by the database */
    ["std.Messages"]       () : DbMessages;
    /** return the JSON Schema assigned to the database */
    ["std.Schema"]         () : DbSchema;
    /** return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]          (param: string | null) : DbStats;
    /** returns descriptive information about the Hub like version, host, project and environment name */
    ["std.Details"]        () : HostDetails;
    /** list all databases and their containers hosted by the Hub */
    ["std.Cluster"]        () : HostCluster;
}

export class GuidEntity {
    id  : Guid;
}

export class IntEntity {
    id  : int32;
}

export class AutoIntEntity {
    id  : int32;
}

export class LongEntity {
    Id  : int64;
}

export class ShortEntity {
    id  : int16;
}

export class ByteEntity {
    id  : uint8;
}

export class CustomIdEntity {
    customId  : string;
}

export class EntityRefs {
    id               : string;
    guidEntity       : Guid;
    guidNullEntity?  : Guid | null;
    intEntity        : int32;
    intNullEntity?   : int32 | null;
    intNullEntity2?  : int32 | null;
    longEntity       : int64;
    longNullEntity?  : int64 | null;
    shortEntity      : int16;
    shortNullEntity? : int16 | null;
    byteEntity       : uint8;
    byteNullEntity?  : uint8 | null;
    customIdEntity?  : string | null;
    intEntities?     : int32[] | null;
    intNullEntities? : (int32 | null)[] | null;
}

export class CustomIdEntity2 {
    customId2  : string;
}

