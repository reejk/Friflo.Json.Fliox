// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int32 }                 from "./Standard"
import { Guid }                  from "./Standard"
import { FilterOperation }       from "./Friflo.Json.Fliox.Transform"
import { FilterOperation_Union } from "./Friflo.Json.Fliox.Transform"
import { JsonPatch }             from "./Friflo.Json.Fliox.Transform"
import { JsonPatch_Union }       from "./Friflo.Json.Fliox.Transform"
import { int64 }                 from "./Standard"

export type ProtocolMessage_Union =
    | SyncRequest
    | SyncResponse
    | ErrorResponse
    | SubscriptionEvent
;

export abstract class ProtocolMessage {
    abstract type:
        | "sync"
        | "syncResp"
        | "error"
        | "sub"
    ;
}

export abstract class ProtocolRequest extends ProtocolMessage {
    reqId? : int32 | null;
}

export class SyncRequest extends ProtocolRequest {
    type    : "sync";
    client? : string | null;
    ack?    : int32 | null;
    token?  : string | null;
    tasks   : SyncRequestTask_Union[];
}

export type SyncRequestTask_Union =
    | CreateEntities
    | UpsertEntities
    | ReadEntitiesList
    | QueryEntities
    | PatchEntities
    | DeleteEntities
    | SendMessage
    | SubscribeChanges
    | SubscribeMessage
    | ReserveKeys
;

export abstract class SyncRequestTask {
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
    ;
}

export class CreateEntities extends SyncRequestTask {
    task           : "create";
    container      : string;
    reservedToken? : Guid | null;
    keyName?       : string | null;
    entities       : any[];
}

export class UpsertEntities extends SyncRequestTask {
    task       : "upsert";
    container  : string;
    keyName?   : string | null;
    entities   : any[];
}

export class ReadEntitiesList extends SyncRequestTask {
    task       : "read";
    container  : string;
    keyName?   : string | null;
    isIntKey?  : boolean | null;
    reads      : ReadEntities[];
}

export class ReadEntities {
    ids         : string[];
    references? : References[] | null;
}

export class References {
    selector    : string;
    container   : string;
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    references? : References[] | null;
}

export class QueryEntities extends SyncRequestTask {
    task        : "query";
    container   : string;
    keyName?    : string | null;
    isIntKey?   : boolean | null;
    filterLinq? : string | null;
    filter?     : FilterOperation_Union | null;
    references? : References[] | null;
}

export class PatchEntities extends SyncRequestTask {
    task       : "patch";
    container  : string;
    keyName?   : string | null;
    patches    : { [key: string]: EntityPatch };
}

export class EntityPatch {
    patches  : JsonPatch_Union[];
}

export class DeleteEntities extends SyncRequestTask {
    task       : "delete";
    container  : string;
    ids?       : string[] | null;
    all?       : boolean | null;
}

export class SendMessage extends SyncRequestTask {
    task   : "message";
    name   : string;
    value  : any;
}

export class SubscribeChanges extends SyncRequestTask {
    task       : "subscribeChanges";
    container  : string;
    changes    : Change[];
    filter?    : FilterOperation_Union | null;
}

export type Change =
    | "create"
    | "upsert"
    | "patch"
    | "delete"
;

export class SubscribeMessage extends SyncRequestTask {
    task    : "subscribeMessage";
    name    : string;
    remove? : boolean | null;
}

export class ReserveKeys extends SyncRequestTask {
    task       : "reserveKeys";
    container  : string;
    count      : int32;
}

export abstract class ProtocolResponse extends ProtocolMessage {
    reqId? : int32 | null;
}

export class SyncResponse extends ProtocolResponse {
    type          : "syncResp";
    tasks?        : SyncTaskResult_Union[] | null;
    results?      : ContainerEntities[] | null;
    createErrors? : { [key: string]: EntityErrors } | null;
    upsertErrors? : { [key: string]: EntityErrors } | null;
    patchErrors?  : { [key: string]: EntityErrors } | null;
    deleteErrors? : { [key: string]: EntityErrors } | null;
}

export type SyncTaskResult_Union =
    | CreateEntitiesResult
    | UpsertEntitiesResult
    | ReadEntitiesListResult
    | QueryEntitiesResult
    | PatchEntitiesResult
    | DeleteEntitiesResult
    | SendMessageResult
    | SubscribeChangesResult
    | SubscribeMessageResult
    | ReserveKeysResult
    | TaskErrorResult
;

export abstract class SyncTaskResult {
    abstract task:
        | "create"
        | "upsert"
        | "read"
        | "query"
        | "patch"
        | "delete"
        | "message"
        | "subscribeChanges"
        | "subscribeMessage"
        | "reserveKeys"
        | "error"
    ;
}

export class CreateEntitiesResult extends SyncTaskResult {
    task   : "create";
    Error? : CommandError | null;
}

export class CommandError {
    message? : string | null;
}

export class UpsertEntitiesResult extends SyncTaskResult {
    task   : "upsert";
    Error? : CommandError | null;
}

export class ReadEntitiesListResult extends SyncTaskResult {
    task   : "read";
    reads  : ReadEntitiesResult[];
}

export class ReadEntitiesResult {
    Error?      : CommandError | null;
    references? : ReferencesResult[] | null;
}

export class ReferencesResult {
    error?      : string | null;
    container?  : string | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

export class QueryEntitiesResult extends SyncTaskResult {
    task        : "query";
    Error?      : CommandError | null;
    container?  : string | null;
    filterLinq? : string | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
}

export class PatchEntitiesResult extends SyncTaskResult {
    task   : "patch";
    Error? : CommandError | null;
}

export class DeleteEntitiesResult extends SyncTaskResult {
    task   : "delete";
    Error? : CommandError | null;
}

export class SendMessageResult extends SyncTaskResult {
    task    : "message";
    Error?  : CommandError | null;
    result? : any | null;
}

export class SubscribeChangesResult extends SyncTaskResult {
    task  : "subscribeChanges";
}

export class SubscribeMessageResult extends SyncTaskResult {
    task  : "subscribeMessage";
}

export class ReserveKeysResult extends SyncTaskResult {
    task   : "reserveKeys";
    Error? : CommandError | null;
    keys?  : ReservedKeys | null;
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

export type TaskErrorResultType =
    | "None"
    | "UnhandledException"
    | "DatabaseError"
    | "InvalidTask"
    | "PermissionDenied"
    | "SyncError"
;

export class ContainerEntities {
    container  : string;
    entities   : any[];
    notFound?  : string[] | null;
    errors?    : { [key: string]: EntityError } | null;
}

export class EntityError {
    type     : EntityErrorType;
    message? : string | null;
}

export type EntityErrorType =
    | "Undefined"
    | "ParseError"
    | "ReadError"
    | "WriteError"
    | "DeleteError"
    | "PatchError"
;

export class EntityErrors {
    container? : string | null;
    errors     : { [key: string]: EntityError };
}

export class ErrorResponse extends ProtocolResponse {
    type     : "error";
    message? : string | null;
}

export abstract class ProtocolEvent extends ProtocolMessage {
    seq     : int32;
    target? : string | null;
    client? : string | null;
}

export class SubscriptionEvent extends ProtocolEvent {
    type    : "sub";
    tasks?  : SyncRequestTask_Union[] | null;
}

