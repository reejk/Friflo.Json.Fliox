// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema

export class ReadEntitiesSet {
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

export class CommandError {
    message? : string | null;
}

export class ReadEntitiesSetResult {
    Error?      : CommandError | null;
    references? : ReferencesResult[] | null;
}

export class ReferencesResult {
    error?      : string | null;
    container?  : string | null;
    ids         : string[];
    references? : ReferencesResult[] | null;
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

