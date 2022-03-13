// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { TaskType } from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { Change }   from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";

/**
 * Each **Role** has a set of **rights**.   
 * Each **Right** is a rule used to grant or deny a specific database operation or command execution.  
 * The database operation or command execution is granted if any of it **rights**
 * grant access.
 */
export type Right_Union =
    | RightAllow
    | RightTask
    | RightSendMessage
    | RightSubscribeMessage
    | RightOperation
    | RightPredicate
;

export abstract class Right {
    abstract type:
        | "allow"
        | "task"
        | "sendMessage"
        | "subscribeMessage"
        | "operation"
        | "predicate"
    ;
    description? : string | null;
}

/**
 * Allow full access to the given **database**.  
 * In case **database** ends with a '*' e.g. 'test*' access to all databases with the prefix 'test'
 * is granted.  
 * Using **database**: '*' grant access to all databases.
 */
export class RightAllow extends Right {
    type         : "allow";
    database?    : string | null;
}

/** **RightTask** grant **database** access by a set of task **types**.    */
export class RightTask extends Right {
    type         : "task";
    database?    : string | null;
    types        : TaskType[];
}

/**
 * **RightSendMessage** allows sending messages to a **database** by a set of **names**.  
 * Each allowed message can be listed explicit in **names**. E.g. 'std.Echo'   
 * A group of messages can be allowed by using a prefix. E.g. 'std.*'   
 * To grant sending every message independent of its name use: '*'    
 * Note: commands are messages - so permission of sending commands is same as for messages.
 */
export class RightSendMessage extends Right {
    type         : "sendMessage";
    database?    : string | null;
    names        : string[];
}

/**
 * **RightSubscribeMessage** allows subscribing messages send to a **database**.  
 * Allow subscribing a specific message by using explicit message **names**. E.g. 'std.Echo'   
 * Allow subscribing a group of messages by using a prefix. E.g. 'std.*'   
 * Allow subscribing all messages by using: '*'    
 * Note: commands are messages - so permission of subscribing commands is same as for messages.
 */
export class RightSubscribeMessage extends Right {
    type         : "subscribeMessage";
    database?    : string | null;
    names        : string[];
}

/**
 * **RightOperation** grant **database** access for the given **containers**
 * based on a set of **operations**.   
 * E.g. create, read, upsert, delete, query or aggregate (count)  
 * It also allows subscribing database changes by **subscribeChanges**
 */
export class RightOperation extends Right {
    type         : "operation";
    database?    : string | null;
    containers   : { [key: string]: ContainerAccess };
}

export class ContainerAccess {
    operations?       : OperationType[] | null;
    subscribeChanges? : Change[] | null;
}

export type OperationType =
    | "create"
    | "upsert"
    | "delete"
    | "deleteAll"
    | "patch"
    | "read"
    | "query"
    | "aggregate"
    | "mutate"
    | "full"
;

export class RightPredicate extends Right {
    type         : "predicate";
    names        : string[];
}

