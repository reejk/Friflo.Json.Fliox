// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import { DateTime } from "./System"

export class Order {
    id        : string;
    customer? : string;
    created?  : DateTime;
    items?    : OrderItem[];
}

export class OrderItem {
    article? : string;
    amount   : number;
    name?    : string;
}

export class Customer {
    id    : string;
    name? : string;
}

export class Article {
    id        : string;
    name?     : string;
    producer? : string;
}

export class Producer {
    id         : string;
    name?      : string;
    employees? : string[];
}

export class Employee {
    id         : string;
    firstName? : string;
    lastName?  : string;
}

