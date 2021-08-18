// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import { Guid }  from "./Standard"
import { int32 } from "./Standard"
import { int64 } from "./Standard"

export abstract class EntityIdStore {
    guidEntities      : { [key: string]: GuidEntity };
    intEntities       : { [key: string]: IntEntity };
    longEntities      : { [key: string]: LongEntity };
    customIdEntities  : { [key: string]: CustomIdEntity };
}

export class GuidEntity {
    id  : Guid;
}

export class IntEntity {
    id  : int32;
}

export class LongEntity {
    Id  : int64;
}

export class CustomIdEntity {
    customId  : string;
}

