// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { TaskType }     from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { EntityChange } from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";

export type TaskRight_Union =
    | DbFullRight
    | DbTaskRight
    | DbContainerRight
    | SendMessageRight
    | SubscribeMessageRight
    | PredicateRight
;

export abstract class TaskRight {
    /** right type */
    abstract type:
        | "dbFull"
        | "dbTask"
        | "dbContainer"
        | "sendMessage"
        | "subscribeMessage"
        | "predicate"
    ;
    /** optional description explaining the Right */
    description? : string | null;
}

/** Allow full access to the given **database**.   */
export class DbFullRight extends TaskRight {
    /** right type */
    type         : "dbFull";
    /** a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*' */
    database     : string;
}

/** **DbTaskRight** grant **database** access by a set of task **types**.    */
export class DbTaskRight extends TaskRight {
    /** right type */
    type         : "dbTask";
    /** a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*' */
    database     : string;
    /** set fo task types like: create, read, upsert, delete, query, ... */
    types        : TaskType[];
}

/**
 * **DbContainerRight** grant **database** access for the given **containers**
 * based on a set of **operations**.   
 * E.g. create, read, upsert, delete, query or aggregate (count)  
 * It also allows subscribing database changes by **subscribeChanges**
 */
export class DbContainerRight extends TaskRight {
    /** right type */
    type         : "dbContainer";
    /** a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*' */
    database     : string;
    /** grant execution of operations and subscriptions on listed **containers** */
    containers   : ContainerAccess[];
}

/** Grant execution of specific container operations and subscriptions */
export class ContainerAccess {
    /** Container name */
    name              : string;
    /** Set of granted operation types */
    operations?       : OperationType[] | null;
    /** Set of granted change subscriptions */
    subscribeChanges? : EntityChange[] | null;
}

export type OperationType =
    | "create"         /** allow to create entities in a container */
    | "upsert"         /** allow to upsert entities in a container */
    | "delete"         /** allow to delete entities in a container */
    | "deleteAll"      /** allow to delete all container entities */
    | "merge"          /** allow to patch entities in a container */
    | "read"           /** allow to read entities in a container */
    | "query"          /** allow to query entities in a container */
    | "aggregate"      /** allow to aggregate - count - entities in a container */
    | "mutate"         /** allow to mutate - create, upsert, delete and patch - entities in a container */
    | "full"           /** allow all operation types in a container */
;

/**
 * **SendMessageRight** allows sending messages to a **database** by a set of **names**.    
 * Note: commands are messages - so permission of sending commands is same as for messages.
 */
export class SendMessageRight extends TaskRight {
    /** right type */
    type         : "sendMessage";
    /** a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*' */
    database     : string;
    /** a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*' */
    names        : string[];
}

/**
 * **SubscribeMessageRight** allows subscribing messages send to a **database**.    
 * Note: commands are messages - so permission of subscribing commands is same as for messages.
 */
export class SubscribeMessageRight extends TaskRight {
    /** right type */
    type         : "subscribeMessage";
    /** a specific database: 'test_db', multiple databases by prefix: 'test_*', all databases: '*' */
    database     : string;
    /** a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*' */
    names        : string[];
}

export class PredicateRight extends TaskRight {
    /** right type */
    type         : "predicate";
    /** a specific predicate: 'TestPredicate', multiple predicates by prefix: 'Test*', all predicates: '*' */
    names        : string[];
}

/** **HubRights** used to set general request / connection permissions. */
export class HubRights {
    /** If **true** the hub store all unacknowledged events for a client in a FIFO queue and send them on reconnects.   */
    queueEvents? : boolean | null;
}

