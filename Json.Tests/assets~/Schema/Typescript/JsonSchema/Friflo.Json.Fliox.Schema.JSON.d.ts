// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int64 } from "./Standard";

/**
 * Compatible subset of JSON Schema with some extensions required for code generation.
 * JSON Schema specification: https://json-schema.org/specification.html
 * 
 * Following extensions are added to JSON Schema:
 * - **extends**
 * - **discriminator**
 * - **isStruct**
 * - **isAbstract**
 * - **messages**
 * - **commands**
 * - **key**
 * - **relation**
 * 
 * The restriction of **JsonSchema** are:
 * - A schema property cannot nest anonymous types by "type": "object" with "properties": { ... }.
 *   The property type needs to be a known type like "string", ... or a referenced ("$ref") type.
 *   This restriction enables generation of code and types for languages without support of anonymous types.
 *   It also enables concise error messages for validation errors when using **TypeValidator**.
 * - Note: Arrays and dictionaries are also valid schema properties. E.g.   
 *   A valid array property like: `{ "type": ["array", "null"], "items": { "type": "string" } }`  
 *   A valid dictionary property like:  `{ "type": "object", "additionalProperties": { "type": "string" } }`  
 *   These element / value types needs to be a known type like "string", ... or a referenced ("$ref") type.
 * - On root level are only "$ref": "..." and "definitions": [...] allowed.
 * 
 */
export class JsonSchema {
    $ref?        : string | null;
    definitions? : { [key: string]: JsonType } | null;
}

export class JsonType {
    extends?              : TypeRef | null;
    discriminator?        : string | null;
    oneOf?                : FieldType[] | null;
    isAbstract?           : boolean | null;
    type?                 : string | null;
    key?                  : string | null;
    properties?           : { [key: string]: FieldType } | null;
    commands?             : { [key: string]: MessageType } | null;
    messages?             : { [key: string]: MessageType } | null;
    isStruct?             : boolean | null;
    required?             : string[] | null;
    additionalProperties  : boolean;
    enum?                 : string[] | null;
    description?          : string | null;
}

export class TypeRef {
    $ref  : string;
    type? : string | null;
}

export class FieldType {
    type?                 : any | null;
    enum?                 : string[] | null;
    items?                : FieldType | null;
    oneOf?                : FieldType[] | null;
    minimum?              : int64 | null;
    maximum?              : int64 | null;
    pattern?              : string | null;
    format?               : string | null;
    $ref?                 : string | null;
    additionalProperties? : FieldType | null;
    isAutoIncrement?      : boolean | null;
    relation?             : string | null;
    description?          : string | null;
}

export class MessageType {
    param?       : FieldType | null;
    result?      : FieldType | null;
    description? : string | null;
}

