// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { Guid }                  from "./Standard";
import { ReadEntitiesSet }       from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { References }            from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { int32 }                 from "./Standard";
import { JsonPatch }             from "./Friflo.Json.Fliox.Transform";
import { JsonPatch_Union }       from "./Friflo.Json.Fliox.Transform";
import { ReadEntitiesSetResult } from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { ReferencesResult }      from "./Friflo.Json.Fliox.Hub.Protocol.Models";
import { double }                from "./Standard";
import { int64 }                 from "./Standard";

/**
 * Polymorphic base type for all tasks.  
 * All tasks fall into two categories:  **container operations** like: create, read, upsert, delete, query, ...  **database operation** like sending commands or messages
 */
export type SyncRequestTask_Union =
    | CreateEntities
    | UpsertEntities
    | ReadEntities
    | QueryEntities
    | AggregateEntities
    | PatchEntities
    | DeleteEntities
    | SendMessage
    | SendCommand
    | CloseCursors
    | SubscribeChanges
    | SubscribeMessage
    | ReserveKeys
;

export abstract class SyncRequestTask {
    /** task type: create, read, upsert, delete, query, aggregate, patch, command, message, subscribeChanges, subscribeMessage */
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "aggregate"
        | "patch"
        | "delete"
        | "message"
        | "command"
        | "closeCursors"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
    ;
    info? : any | null;
}

/** Create the given **entities** in the specified **container** */
export class CreateEntities extends SyncRequestTask {
    task           : "create";
    /** container name the **entities** are created */
    container      : string;
    reservedToken? : Guid | null;
    /** name of the primary key property in **entities** */
    keyName?       : string | null;
    /** the **entities** which are created in the specified **container** */
    entities       : any[];
}

/** Upsert the given **entities** in the specified **container** */
export class UpsertEntities extends SyncRequestTask {
    task       : "upsert";
    /** container name the **entities** are upserted - created or updated */
    container  : string;
    /** name of the primary key property in **entities** */
    keyName?   : string | null;
    /** the **entities** which are upserted in the specified **container** */
    entities   : any[];
}

/**
 * Read entities by id from the specified **container** using read **sets**  
 * Each **ReadEntitiesSet** contains a list of **ids**  
 * To return also entities referenced by entities listed in **ids** use
 * **references** in **sets**.   
 * This mimic the functionality of a **JOIN** in **SQL**
 */
export class ReadEntities extends SyncRequestTask {
    task       : "read";
    /** container name */
    container  : string;
    /** name of the primary key property of the returned entities */
    keyName?   : string | null;
    isIntKey?  : boolean | null;
    /** contains the **ids** of requested entities */
    sets       : ReadEntitiesSet[];
}

/**
 * Query entities from the given **container** using a **filter**  
 * To return entities referenced by fields of the query result use **references**
 */
export class QueryEntities extends SyncRequestTask {
    task        : "query";
    /** container name */
    container   : string;
    /** name of the primary key property of the returned entities */
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    /**
     * query filter as JSON tree.   
     * Is used in favour of **filter** as its serialization is more performant
     */
    filterTree? : any | null;
    /**
     * query filter as a [Lambda expression](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions) (infix notation)
     * returning a boolean value. E.g. o.name == 'Smartphone'  **filterTree** has priority if given
     */
    filter?     : string | null;
    /** used to request the entities referenced by properties of the query task result */
    references? : References[] | null;
    /** limit the result set to the given number */
    limit?      : int32 | null;
    /** execute a cursor request with the specified **maxCount** number of entities in the result. */
    maxCount?   : int32 | null;
    /** specify the **cursor** of a previous cursor request */
    cursor?     : string | null;
}

/** Aggregate - count - entities from the given **container** using a **filter**   */
export class AggregateEntities extends SyncRequestTask {
    task        : "aggregate";
    /** container name */
    container   : string;
    /** aggregation type - e.g. count */
    type        : AggregateType;
    /**
     * aggregation filter as JSON tree.   
     * Is used in favour of **filter** as its serialization is more performant
     */
    filterTree? : any | null;
    /**
     * aggregation filter as a [Lambda expression](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions) (infix notation)
     * returning a boolean value. E.g. o.name == 'Smartphone'  
     */
    filter?     : string | null;
}

/** Aggregation type used in **AggregateEntities** */
export type AggregateType =
    | "count"      /** count entities */
;

/**
 * Patch entities by id in the given **container**  
 * Each **EntityPatch** in **patches** contains a set of **patches** for each entity.
 */
export class PatchEntities extends SyncRequestTask {
    task       : "patch";
    /** container name */
    container  : string;
    /** name of the primary key property of the entity **patches** */
    keyName?   : string | null;
    /** set of patches for each entity identified by its primary key */
    patches    : { [key: string]: EntityPatch };
}

/** Contains the **patches** applied to an entity. Used by **PatchEntities** */
export class EntityPatch {
    /** list of patches applied to an entity */
    patches  : JsonPatch_Union[];
}

/**
 * Delete entities by id in the given **container**  
 * The entities which will be deleted are listed in **ids**
 */
export class DeleteEntities extends SyncRequestTask {
    task       : "delete";
    /** container name */
    container  : string;
    /** list of **ids** requested for deletion */
    ids?       : string[] | null;
    /** if true all entities in the specified **container** are deleted */
    all?       : boolean | null;
}

/**
 * Used as base type for **SendMessage** or **SendCommand** to specify the command / message
 * **name** and **param**
 */
export abstract class SyncMessageTask extends SyncRequestTask {
    /** command / message name */
    name   : string;
    /** command / message parameter. Can be null or absent */
    param? : any | null;
}

/** Send a database message with the given **param**   */
export class SendMessage extends SyncMessageTask {
    task   : "message";
}

/** Send a database command with the given **param**   */
export class SendCommand extends SyncMessageTask {
    task   : "command";
}

/** Close the **cursors** of the given **container** */
export class CloseCursors extends SyncRequestTask {
    task       : "closeCursors";
    /** container name */
    container  : string;
    /** list of **cursors** */
    cursors?   : string[] | null;
}

/** Subscribe to specific **changes** of the specified **container** using the given **filter** */
export class SubscribeChanges extends SyncRequestTask {
    task       : "subscribeChanges";
    /** container name */
    container  : string;
    /** type of entity **changes** to be subscribed */
    changes    : Change[];
    /**
     * subscription filter as a [Lambda expression](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions) (infix notation)
     * returning a boolean value. E.g. o.name == 'Smartphone'
     */
    filter?    : any | null;
}

/** Filter type used to specify the type of a database change. */
export type Change =
    | "create"      /** filter change events of created entities. */
    | "upsert"      /** filter change events of upserted entities. */
    | "patch"       /** filter change events of entity patches. */
    | "delete"      /** filter change events of deleted entities. */
;

/**
 * Subscribe to commands and messages sent to a database by their **name**  
 * Unsubscribe by setting **remove** to true
 */
export class SubscribeMessage extends SyncRequestTask {
    task    : "subscribeMessage";
    /**
     * Subscribe all messages with the given **name**.   
     * Subscribe all messages         **name** = '*'  
     * Subscribe messages with prefix **name** = 'std.*'   
     * Subscribe a specific message   **name** = 'std.Echo'
     */
    name    : string;
    /** if true a previous added subscription is removed. Otherwise added */
    remove? : boolean | null;
}

/** WIP */
export class ReserveKeys extends SyncRequestTask {
    task       : "reserveKeys";
    container  : string;
    count      : int32;
}

export type SyncTaskResult_Union =
    | CreateEntitiesResult
    | UpsertEntitiesResult
    | ReadEntitiesResult
    | QueryEntitiesResult
    | AggregateEntitiesResult
    | PatchEntitiesResult
    | DeleteEntitiesResult
    | SendMessageResult
    | SendCommandResult
    | CloseCursorsResult
    | SubscribeChangesResult
    | SubscribeMessageResult
    | ReserveKeysResult
    | TaskErrorResult
;

export abstract class SyncTaskResult {
    /** task result type */
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "aggregate"
        | "patch"
        | "delete"
        | "message"
        | "command"
        | "closeCursors"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
        | "error"
    ;
}

export class CreateEntitiesResult extends SyncTaskResult {
    task  : "create";
}

export class UpsertEntitiesResult extends SyncTaskResult {
    task  : "upsert";
}

export class ReadEntitiesResult extends SyncTaskResult {
    task  : "read";
    sets  : ReadEntitiesSetResult[];
}

export class QueryEntitiesResult extends SyncTaskResult {
    task        : "query";
    container?  : string | null;
    cursor?     : string | null;
    /** number of **ids** - not utilized by Protocol */
    count?      : int32 | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

export class AggregateEntitiesResult extends SyncTaskResult {
    task       : "aggregate";
    container? : string | null;
    value?     : double | null;
}

export class PatchEntitiesResult extends SyncTaskResult {
    task  : "patch";
}

export class DeleteEntitiesResult extends SyncTaskResult {
    task  : "delete";
}

export abstract class SyncMessageResult extends SyncTaskResult {
}

export class SendMessageResult extends SyncMessageResult {
    task  : "message";
}

export class SendCommandResult extends SyncMessageResult {
    task    : "command";
    result? : any | null;
}

export class CloseCursorsResult extends SyncTaskResult {
    task   : "closeCursors";
    count  : int32;
}

export class SubscribeChangesResult extends SyncTaskResult {
    task  : "subscribeChanges";
}

export class SubscribeMessageResult extends SyncTaskResult {
    task  : "subscribeMessage";
}

export class ReserveKeysResult extends SyncTaskResult {
    task  : "reserveKeys";
    keys? : ReservedKeys | null;
}

export class ReservedKeys {
    start  : int64;
    count  : int32;
    token  : Guid;
}

export class TaskErrorResult extends SyncTaskResult {
    task        : "error";
    type        : TaskErrorResultType;
    message?    : string | null;
    stacktrace? : string | null;
}

/** Type of a task error used in **TaskErrorResult** */
export type TaskErrorResultType =
    | "None"
    | "UnhandledException"      /**
       * Unhandled exception while executing a task.  
       * maps to HTTP status: 500
       */
    | "DatabaseError"           /**
       * General database error while task execution.  
       * E.g. the access is currently not available or accessing a missing table.  
       * maps to HTTP status: 500
       */
    | "FilterError"             /** Invalid query filter      maps to HTTP status: 400 */
    | "ValidationError"         /** Schema validation of an entity failed     maps to HTTP status: 400 */
    | "CommandError"            /** Execution of message / command failed caused by invalid input    maps to HTTP status: 400 */
    | "InvalidTask"             /** Invalid task. E.g. by using an invalid task parameter    maps to HTTP status: 400 */
    | "NotImplemented"          /** database message / command not implemented       maps to HTTP status: 501 */
    | "PermissionDenied"        /** task execution not authorized    maps to HTTP status: 403 */
    | "SyncError"               /** The entire **SyncRequest** containing a task failed    maps to HTTP status: 500 */
;

