// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import { Entity }     from "./Friflo.Json.Flow.Graph"
import { DateTime }   from "./Standard"
import { int32 }      from "./Standard"
import { BigInteger } from "./Standard"
import { uint8 }      from "./Standard"
import { int16 }      from "./Standard"
import { int64 }      from "./Standard"
import { float }      from "./Standard"
import { double }     from "./Standard"

export abstract class PocStore {
    orders?    : { [key: string]: Order } | null;
    customers? : { [key: string]: Customer } | null;
    articles?  : { [key: string]: Article } | null;
    producers? : { [key: string]: Producer } | null;
    employees? : { [key: string]: Employee } | null;
    types?     : { [key: string]: TestType } | null;
}

export class Order extends Entity {
    customer? : string | null;
    created   : DateTime;
    items?    : OrderItem[] | null;
}

export class OrderItem {
    article  : string;
    amount   : int32;
    name?    : string | null;
}

export class Customer extends Entity {
    name  : string;
}

export class Article extends Entity {
    name      : string;
    producer? : string | null;
}

export class Producer extends Entity {
    name       : string;
    employees? : string[] | null;
}

export class Employee extends Entity {
    firstName  : string;
    lastName?  : string | null;
}

export class TestType extends Entity {
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
    jsonValue?        : any | null;
    derivedClass      : DerivedClass;
    derivedClassNull? : DerivedClass | null;
}

export class PocStruct {
    value  : int32;
}

export class DerivedClass extends OrderItem {
    derivedVal  : int32;
}

