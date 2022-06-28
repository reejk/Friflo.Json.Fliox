// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }        from "./Standard";
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserOptions }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserResult }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DateTime }     from "./Standard";
import { BigInteger }   from "./Standard";
import { uint8 }        from "./Standard";
import { int16 }        from "./Standard";
import { int64 }        from "./Standard";
import { float }        from "./Standard";
import { double }       from "./Standard";

/**
 * The **PocStore** offer two functionalities:   
 * 1. Defines a database **schema** by declaring its containers, commands and messages  
 * 2. Is a database **client** providing type-safe access to its containers, commands and messages   
 */
// schema documentation only - not implemented right now
export interface PocStore {
    // --- containers
    orders     : { [key: string]: Order };
    customers  : { [key: string]: Customer };
    articles   : { [key: string]: Article };
    producers  : { [key: string]: Producer };
    employees  : { [key: string]: Employee };
    types      : { [key: string]: TestType };

    // --- commands
    ["TestCommand"]                        (param: TestCommand | null) : boolean;
    ["SyncCommand"]                        (param: string | null) : string;
    ["AsyncCommand"]                       (param: string | null) : string;
    ["Command1"]                           () : string;
    ["CommandInt"]                         (param: int32) : int32;
    ["test.Command2"]                      () : string;
    ["test.CommandHello"]                  (param: string | null) : string;
    ["test.CommandExecutionError"]         () : int32;
    ["test.CommandExecutionException"]     () : int32;
    /** echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]                           (param: any) : any;
    /** list all database containers */
    ["std.Containers"]                     () : DbContainers;
    /** list all database commands and messages */
    ["std.Messages"]                       () : DbMessages;
    /** return the Schema assigned to the database */
    ["std.Schema"]                         () : DbSchema;
    /** return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]                          (param: string | null) : DbStats;
    /** returns general information about the Hub like version, host, project and environment name */
    ["std.Details"]                        () : HostDetails;
    /** list all databases and their containers hosted by the Hub */
    ["std.Cluster"]                        () : HostCluster;
    /** change and return user groups */
    ["std.User"]                           (param: UserOptions | null) : UserResult;

    // --- messages
    ["Message1"]          (param: string | null) : void;
    ["AsyncMessage"]      (param: string | null) : void;
    ["test.Message2"]     (param: string | null) : void;
}

/**
 * Some useful class documentation :)
 * ```
 * multiline line
 * code documentation
 * ```
 * Test type reference '**OrderItem**'
 */
export class Order {
    id        : string;
    /**
     * Some **useful** field documentation 🙂
     * Check some new lines
     * in documentation
     */
    customer? : string | null;
    /** single line documentation */
    created   : DateTime;
    /** `single line code documentation` */
    items?    : OrderItem[] | null;
}

export class Customer {
    id    : string;
    name  : string;
}

export class Article {
    id        : string;
    name      : string;
    producer? : string | null;
}

export class Producer {
    id         : string;
    name       : string;
    employees? : string[] | null;
}

export class Employee {
    id         : string;
    firstName  : string;
    lastName?  : string | null;
}

export abstract class PocEntity {
    id  : string;
}

export class TestType extends PocEntity {
    dateTime          : DateTime;
    dateTimeNull?     : DateTime | null;
    bigInt            : BigInteger;
    bigIntNull?       : BigInteger | null;
    boolean           : boolean;
    booleanNull?      : boolean | null;
    uint8             : uint8;
    uint8Null?        : uint8 | null;
    int16             : int16;
    int16Null?        : int16 | null;
    int32             : int32;
    int32Null?        : int32 | null;
    int64             : int64;
    int64Null?        : int64 | null;
    float32           : float;
    float32Null?      : float | null;
    float64           : double;
    float64Null?      : double | null;
    pocStruct         : PocStruct;
    pocStructNull?    : PocStruct | null;
    intArray          : int32[];
    intArrayNull?     : int32[] | null;
    intNullArray?     : (int32 | null)[] | null;
    jsonValue?        : any | null;
    derivedClass      : DerivedClass;
    derivedClassNull? : DerivedClass | null;
}

export class OrderItem {
    article  : string;
    amount   : int32;
    name?    : string | null;
}

export class PocStruct {
    value  : int32;
}

export class DerivedClass extends OrderItem {
    derivedVal  : int32;
}

export class TestCommand {
    text? : string | null;
}

