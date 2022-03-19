// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }                 from "./Standard";
import { SyncRequestTask }       from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { SyncRequestTask_Union } from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { SyncTaskResult }        from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { SyncTaskResult_Union }  from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";
import { EntityError }           from "./Friflo.Json.Fliox.Hub.Protocol.Models";

/**
 * **ProtocolMessage** is the base type for all messages which are classified into request, response and event.
 * It can also be used in communication going beyond the request / response schema.
 *   
 * A **ProtocolMessage** is either one of the following types:
 * - **ProtocolRequest**  send by clients / received by hosts
 * - **ProtocolResponse** send by hosts / received by clients
 * - **ProtocolEvent**    send by hosts / received by clients
 * *Note*: By applying this classification the protocol can also be used in peer-to-peer networking.
 * 
 * General principle of **Fliox** message protocol:  
 * All messages like requests (their tasks), responses (their results) and events are stateless.  
 * In other words: All messages are self-contained and doesnt (and must not) rely and previous sent messages.
 * The technical aspect of having a connection e.g. HTTP or WebSocket is not relevant.
 * This enables two fundamental features:  
 * 1. embedding all messages in various communication protocols like HTTP, WebSockets, TCP, WebRTC or datagram based protocols.  
 * 2. multiplexing of messages from different clients, servers or peers in a shared connection.  
 * This also means all **Fliox** messages doesnt (and must not) require a session.  
 * This principle also enables using a single **FlioxHub** by multiple clients like
 * **FlioxClient** even for remote clients like **RemoteClientHub**.
 * 
 */
export type ProtocolMessage_Union =
    | SyncRequest
    | SyncResponse
    | ErrorResponse
    | EventMessage
;

export abstract class ProtocolMessage {
    /** message type */
    abstract msg:
        | "sync"
        | "resp"
        | "error"
        | "ev"
    ;
}

export type ProtocolRequest_Union =
    | SyncRequest
;

export abstract class ProtocolRequest extends ProtocolMessage {
    /** request type */
    abstract msg:
        | "sync"
    ;
    /**
     * Used only for **RemoteClientHub** to enable:
     * 
     * 1. Out of order response handling for their corresponding requests.
     * 
     * 2. Multiplexing of requests and their responses for multiple clients e.g. **FlioxClient**
     * using the same connection.
     * This is not a common scenario but it enables using a single **WebSocketClientHub**
     * used by multiple clients.
     * 
     * The host itself only echos the **reqId** to **reqId** and
     * does **not** utilize it internally.
     */
    req? : int32 | null;
    /**
     * As a user can access a **FlioxHub** by multiple clients the **clientId**
     * enables identifying each client individually.   
     * The **clientId** is used for **SubscribeMessage** and **SubscribeChanges**
     * to enable sending **EventMessage**'s to the desired subscriber.
     */
    clt? : string | null;
}

export class SyncRequest extends ProtocolRequest {
    msg       : "sync";
    /**
     * Identify the user performing a sync request.
     * In case using of using **UserAuthenticator** the **userId** and **token**
     * are use for user authentication.
     */
    user?     : string | null;
    token?    : string | null;
    /**
     * **eventAck** is used to ensure (change) events are delivered reliable.
     * A client set **eventAck** to the last received **seq** in case
     * it has subscribed to database changes by a **SubscribeChanges** task.
     * Otherwise **eventAck** is null.
     */
    ack?      : int32 | null;
    /** list of tasks either container operations or database commands / messages */
    tasks     : SyncRequestTask_Union[];
    /** database name the **tasks** apply to. null to access the default database */
    database? : string | null;
    /** optional JSON value - can be used to describe a request */
    info?     : any | null;
}

/**
 * Base type for response messages send from a host to a client in reply of **SyncRequest**  
 * A response is either a **SyncResponse** or a **ErrorResponse** in case of a general error.
 */
export type ProtocolResponse_Union =
    | SyncResponse
    | ErrorResponse
;

export abstract class ProtocolResponse extends ProtocolMessage {
    /** response type */
    abstract msg:
        | "resp"
        | "error"
    ;
    /** Set to the value of the corresponding **reqId** of a **ProtocolRequest** */
    req? : int32 | null;
    /**
     * Set to **clientId** of a **SyncRequest** in case the given
     * **clientId** was valid. Otherwise it is set to null.   
     * Calling **EnsureValidClientId()** when **clientId** == null a
     * new unique client id will be assigned.   
     * For tasks which require a **clientId** a client need to set **clientId**
     * to **clientId**.   
     * This enables tasks like **SubscribeMessage** or **SubscribeChanges** identifying the
     * **EventMessage** target.
     */
    clt? : string | null;
}

/** The response send back from a host in reply of a **SyncRequest** */
export class SyncResponse extends ProtocolResponse {
    msg           : "resp";
    /** for debugging - not used by Protocol */
    database?     : string | null;
    /** list of task results corresponding to the **tasks** in a **SyncRequest** */
    tasks?        : SyncTaskResult_Union[] | null;
    /**
     * entities as results from the **tasks** in a **SyncRequest**
     * grouped by container
     */
    containers?   : ContainerEntities[] | null;
    /** errors caused by **CreateEntities** tasks grouped by container */
    createErrors? : { [key: string]: EntityErrors } | null;
    /** errors caused by **UpsertEntities** tasks grouped by container */
    upsertErrors? : { [key: string]: EntityErrors } | null;
    /** errors caused by **PatchEntities** tasks grouped by container */
    patchErrors?  : { [key: string]: EntityErrors } | null;
    /** errors caused by **DeleteEntities** tasks grouped by container */
    deleteErrors? : { [key: string]: EntityErrors } | null;
    /** optional JSON value to return debug / development data - e.g. execution times or resource usage. */
    info?         : any | null;
}

/**
 * Used by **SyncResponse** to return the **entities** as results
 * from **tasks** of a **SyncRequest**
 */
export class ContainerEntities {
    /** container name the of the returned **entities** */
    container  : string;
    /** number of **entities** - not utilized by Protocol */
    count?     : int32 | null;
    /** all **entities** as results from **tasks** of a **SyncRequest** */
    entities   : any[];
    /** list of entities not found by **ReadEntities** tasks */
    notFound?  : string[] | null;
    /** list of errors when accessing entities from a database */
    errors?    : { [key: string]: EntityError } | null;
}

export class EntityErrors {
    container? : string | null;
    errors     : { [key: string]: EntityError };
}

export class ErrorResponse extends ProtocolResponse {
    msg      : "error";
    message? : string | null;
    type     : ErrorResponseType;
}

export type ErrorResponseType =
    | "BadRequest"       /** Invalid JSON request or invalid request parameters. Maps to HTTP status code 400 (Bad Request) */
    | "Exception"        /** Internal exception. Maps to HTTP status code 500 (Internal Server Error) */
    | "BadResponse"      /** Invalid JSON response. Maps to HTTP status code 500 (Internal Server Error) */
;

export type ProtocolEvent_Union =
    | EventMessage
;

export abstract class ProtocolEvent extends ProtocolMessage {
    /** event type */
    abstract msg:
        | "ev"
    ;
    seq  : int32;
    src  : string;
    clt  : string;
}

export class EventMessage extends ProtocolEvent {
    msg    : "ev";
    /**
     * Contains the events an application subscribed. These are:
     * - **CreateEntities**
     * - **UpsertEntities**
     * - **DeleteEntities**
     * - **PatchEntities**
     * - **SendMessage**
     * - **SendCommand**
     * 
     */
    tasks? : SyncRequestTask_Union[] | null;
}

