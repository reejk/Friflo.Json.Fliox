// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int64 } from "./Standard";

/**
 * Compatible subset of JSON Schema with some extensions required for code generation.  
 * JSON Schema specification: https://json-schema.org/specification.html    
 * Following extensions are added to JSON Schema:
 * - **extends** - used to declare that a typ definition extends the given one
 * - **discriminator** - declare the property name used as discriminator
 * - **isStruct** - type should be generated as struct
 * - **isAbstract** - type definition is an abstract type
 * - **messages** - list of all schema messages
 * - **commands** - list of all schema commands
 * - **key** - the property used as primary key
 * - **descriptions** - a map storing the descriptions for enum values
 * - **relation** - mark the property as a relation (aka reference or aka secondary key) to entities in the given container
 * 
 * The restriction of **JSONSchema** are:
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
export class JSONSchema {
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
    descriptions?         : { [key: string]: string } | null;
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

