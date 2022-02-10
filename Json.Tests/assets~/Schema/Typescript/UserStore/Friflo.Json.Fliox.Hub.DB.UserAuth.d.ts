// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbCommands }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HubInfo }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HubCluster }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { Right }        from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";
import { Right_Union }  from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";

export interface UserStore {
    // --- containers
    credentials  : { [key: string]: UserCredential };
    permissions  : { [key: string]: UserPermission };
    roles        : { [key: string]: Role };

    // --- commands
    ["AuthenticateUser"]     (param: AuthenticateUser) : AuthenticateUserResult;
    ["std.DbEcho"]           (param: any) : any;
    ["std.DbContainers"]     (param: any) : DbContainers;
    ["std.DbCommands"]       (param: any) : DbCommands;
    ["std.DbSchema"]         (param: any) : DbSchema;
    ["std.DbStats"]          (param: any) : DbStats;
    ["std.HubInfo"]          (param: any) : HubInfo;
    ["std.HubCluster"]       (param: any) : HubCluster;
}

export class UserCredential {
    id     : string;
    token? : string | null;
}

export class UserPermission {
    id     : string;
    roles? : string[] | null;
}

export class Role {
    id           : string;
    rights       : Right_Union[];
    description? : string | null;
}

export class AuthenticateUser {
    userId  : string;
    token   : string;
}

export class AuthenticateUserResult {
    isValid  : boolean;
}

