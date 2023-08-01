// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { Order }   from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { JsonKey } from "./Standard";
import { int32 }   from "./Standard";

/** **References** are used to return entities referenced by fields of entities returned by read and query tasks.  **References** can be nested to return referenced entities of referenced entities. */
export class References {
    /** the field path used as a reference to an entity in the specified **container** */
    selector    : string;
    /** the **container** storing the entities referenced by the specified **selector** */
    cont        : string;
    orderByKey? : Order | null;
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    references? : References[] | null;
}

/** Used by **SyncResponse** to return errors when mutating an entity by: create, upsert, patch and delete */
export class EntityError {
    /** entity id */
    id       : JsonKey;
    /** error type when accessing an entity in a database */
    type     : EntityErrorType;
    /** error details when accessing an entity */
    message? : string | null;
}

/** Error type when accessing an entity from a database container */
export type EntityErrorType =
    | "Undefined"
    | "ParseError"       /**
       * Invalid JSON when reading an entity from database  
       * can happen with key-value databases - e.g. file-system - as their values are not restricted to JSON
       */
    | "ReadError"        /**
       * Reading an entity from database failed  
       * e.g. a corrupt file when using the file-system as database
       */
    | "WriteError"       /**
       * Writing an entity to database failed  
       * e.g. the file is already in use by another process when using the file-system as database
       */
    | "DeleteError"      /**
       * Deleting an entity in database failed  
       * e.g. the file is already in use by another process when using the file-system as database
       */
    | "PatchError"       /** Patching an entity failed */
;

export class ReferencesResult {
    error?      : string | null;
    /** container name - not utilized by Protocol */
    cont?       : string | null;
    /** number of **set** entries - not utilized by Protocol */
    len?        : int32 | null;
    set         : any[];
    errors?     : EntityError[] | null;
    references? : ReferencesResult[] | null;
}

