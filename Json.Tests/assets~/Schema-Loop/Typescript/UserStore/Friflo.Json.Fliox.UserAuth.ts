// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { Right }       from "./Friflo.Json.Fliox.Auth.Rights"
import { Right_Union } from "./Friflo.Json.Fliox.Auth.Rights"

export abstract class UserStore {
    permissions  : { [key: string]: UserPermission };
    credentials  : { [key: string]: UserCredential };
    roles        : { [key: string]: Role };
}

export class Role {
    id           : string;
    rights       : Right_Union[];
    description? : string | null;
}

export class UserCredential {
    id        : string;
    passHash? : string | null;
    token?    : string | null;
}

export class UserPermission {
    id     : string;
    roles? : string[] | null;
}

