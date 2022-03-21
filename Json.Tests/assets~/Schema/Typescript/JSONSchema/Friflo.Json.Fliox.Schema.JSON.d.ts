// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int64 } from "./Standard";

/**
 * Compatible subset of the [JSON Schema specification](https://json-schema.org/specification.html) with some extensions to
 * - enable **code generation** for various languages
 * - define **database schemas** declaring its **containers**, **commands** and **messages**
 *   
 * Following extensions are added to the specification:
 * - **extends** - used to declare that a type definition extends the given one
 * - **discriminator** - declare the property name used as discriminator
 * - **isStruct** - type should be generated as struct - value type
 * - **isAbstract** - type definition is an abstract type
 * - **messages** - list of all database messages
 * - **commands** - list of all database commands
 * - **key** - name of the property used as primary key
 * - **descriptions** - a map storing the descriptions for enum values
 * - **relation** - mark the property as a relation (aka reference or aka secondary key) to entities in the container named relation
 * 
 * The restriction of **JSONSchema** are:
 * - A schema property cannot nest anonymous types by "type": "object" with "properties": { ... }.   
 *   The property type needs to be a known type like "string", ... or a referenced **"$ref"** type.    
 *   This restriction enables generation of code and types for languages without support of anonymous types.   
 *   It also enables concise error messages for validation errors when using **TypeValidator**.
 * - Note: Arrays and dictionaries are also valid schema properties. E.g.   
 *   A valid array property like: `{ "type": ["array", "null"], "items": { "type": "string" } }`  
 *   A valid dictionary property like:  `{ "type": "object", "additionalProperties": { "type": "string" } }`  
 *   These element / value types needs to be a known type like "string", ... or a referenced **"$ref"** type.
 * - On root level are only "$ref": "..." and "definitions": [...] allowed.
 * 
 */
export class JSONSchema {
    /**
     * reference to 'main' type definition in **definitions** to  
     * enable schema urls without fragment suffix like: #/definitions/SomeType
     */
    $ref?        : string | null;
    /** map of type **definitions** contained by the JSON Schema. */
    definitions? : { [key: string]: JsonType } | null;
}

/** Use by **definitions** in **JSONSchema** to declare a type definition */
export class JsonType {
    /** reference to type definition which **extends** this type - *JSON Schema extension* */
    extends?              : TypeRef | null;
    /** **discriminator** declares the name of the property used for polymorphic types - *JSON Schema extension* */
    discriminator?        : string | null;
    /** list of all specific types a polymorphic type can be. Is required if **discriminator** is assigned */
    oneOf?                : FieldType[] | null;
    /** declare type as an abstract type - *JSON Schema extension* */
    isAbstract?           : boolean | null;
    /** a basic JSON Schema type: 'null', 'object', 'string', 'boolean', 'number', 'integer' or 'array' */
    type?                 : string | null;
    /** name of the property used as primary **key** for entities - *JSON Schema extension* */
    key?                  : string | null;
    /**
     * map of all **properties** declared by the type definition:  
     * - its keys are the property names  
     * - its values are property types.  
     * in case of a database schema the **properties** declare the database **containers**
     */
    properties?           : { [key: string]: FieldType } | null;
    /**
     * map of database **commands** - *JSON Schema extension*  
     * - its keys are the command names  
     * - its values the command signatures
     */
    commands?             : { [key: string]: MessageType } | null;
    /**
     * map of database database **messages** - *JSON Schema extension*  
     * - its keys are the message names  
     * - its values the message signatures
     */
    messages?             : { [key: string]: MessageType } | null;
    /** true if type should be generated as a value type (struct) - *JSON Schema extension* */
    isStruct?             : boolean | null;
    /** list of **required** properties */
    required?             : string[] | null;
    /** true if **additionalProperties** are allowed */
    additionalProperties  : boolean;
    /** all values that can be used for an enumeration type */
    enum?                 : string[] | null;
    /** map of optional **descriptions** for **enum** values - *JSON Schema extension* */
    descriptions?         : { [key: string]: string } | null;
    /** optional type description */
    description?          : string | null;
}

/** A reference to a type definition in a JSON Schema */
export class TypeRef {
    /** reference to a type definition */
    $ref  : string;
}

/** Defines the type of property */
export class FieldType {
    /**
     * a basic JSON Schema type: 'null', 'object', 'string', 'boolean', 'number', 'integer' or 'array'  
     * or an array of these types used to declare **nullable** properties when using a basic JSON Schema type
     */
    type?                 : any | null;
    /** discriminant of a specific polymorphic type. Always an array with one string element */
    enum?                 : string[] | null;
    /** if set the property is an array - it declares the type of its **items** */
    items?                : FieldType | null;
    /** list of valid property types - used to declare **nullable** properties when using a **$ref** type */
    oneOf?                : FieldType[] | null;
    /** **minimum** valid number */
    minimum?              : int64 | null;
    /** **maximum** valid number */
    maximum?              : int64 | null;
    /** regular expression **pattern** to constrain string values */
    pattern?              : string | null;
    /** set to **'date-time'** if the property is a timestamp formatted as RFC 3339 + milliseconds */
    format?               : string | null;
    /** reference to type definition used as property type */
    $ref?                 : string | null;
    /**
     * if set the property is a map (Dictionary) using the key type **string** and the value type
     * specified by **additionalProperties**
     */
    additionalProperties? : FieldType | null;
    /** WIP */
    isAutoIncrement?      : boolean | null;
    /** if set the property is used as reference to entities in a database **container** named **relation** - *JSON Schema extension* */
    relation?             : string | null;
    /** optional property description */
    description?          : string | null;
}

/** Defines the input **param** and **result** of a command or message */
export class MessageType {
    /** type of the command / message **param** - *JSON Schema extension* */
    param?       : FieldType | null;
    /**
     * type of the command **result** - *JSON Schema extension*  
     * messages return no **result**
     */
    result?      : FieldType | null;
    /** optional command / message description */
    description? : string | null;
}

