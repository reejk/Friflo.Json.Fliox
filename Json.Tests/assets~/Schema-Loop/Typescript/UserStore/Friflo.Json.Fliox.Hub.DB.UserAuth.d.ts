// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }           from "./Standard";
import { DbContainers }    from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }        from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }         from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostParam }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostInfo }        from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserParam }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserResult }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { ClientParam }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { ClientResult }    from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { TaskRight }       from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";
import { TaskRight_Union } from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";
import { HubRights }       from "./Friflo.Json.Fliox.Hub.Host.Auth.Rights";

/**
 * Control individual user access to database containers and commands.   
 * Each **user** has a set of **roles** stored in container **permissions**.   
 * Each **role** in container **roles** has a set of **rights** which grant or deny container access or command execution.
 */
// schema documentation only - not implemented right now
export interface UserStore {
    // --- containers
    credentials  : { [key: string]: UserCredential };
    permissions  : { [key: string]: UserPermission };
    roles        : { [key: string]: Role };
    targets      : { [key: string]: UserTarget };

    // --- commands
    /** authenticate user **Credentials**: **userId** and **token** */
    ["AuthenticateUser"]     (param: Credentials | null) : AuthResult;
    ["ValidateUserDb"]       () : ValidateUserDbResult;
    ["ClearAuthCache"]       () : boolean;
    /** Echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]             (param: any) : any;
    /** A command that completes after a specified number of milliseconds. */
    ["std.Delay"]            (param: int32) : int32;
    /** List all database containers */
    ["std.Containers"]       () : DbContainers;
    /** List all database commands and messages */
    ["std.Messages"]         () : DbMessages;
    /** Return the Schema assigned to the database */
    ["std.Schema"]           () : DbSchema;
    /** Return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]            (param: string | null) : DbStats;
    /** Returns general information about the Hub like version, host, project and environment name */
    ["std.Host"]             (param: HostParam | null) : HostInfo;
    /** List all databases and their containers hosted by the Hub */
    ["std.Cluster"]          () : HostCluster;
    /** Return the groups of the current user. Optionally change the groups of the current user */
    ["std.User"]             (param: UserParam | null) : UserResult;
    /** Return client specific infos and adjust general client behavior like **queueEvents** */
    ["std.Client"]           (param: ClientParam | null) : ClientResult;
}

/** user **Credentials** used for authentication */
export class Credentials {
    userId  : string;
    token   : string;
}

/** Result of **AuthenticateUser()** command */
export class AuthResult {
    /** true if authentication was successful */
    isValid  : boolean;
}

export class ValidateUserDbResult {
    errors? : string[] | null;
}

/** Contains a set of **taskRights** used for task authorization */
export class Role {
    /** **Role** name */
    id           : string;
    /** a set of **taskRights** used for task authorization */
    taskRights   : TaskRight_Union[];
    /** general request / connection rights for Hub access */
    hubRights?   : HubRights | null;
    /** optional **description** explaining a **Role** */
    description? : string | null;
}

/** contains a **token** assigned to a user used for authentication */
export class UserCredential {
    /** user id */
    id     : string;
    /** user token */
    token? : string | null;
}

/** Set of **roles** assigned to a user used for authorization */
export class UserPermission {
    /** user id */
    id     : string;
    /** set of **roles** assigned to a user */
    roles  : string[];
}

/**
 * contain the **groups** assigned to a user.  
 * These groups are used to enable forwarding of message events only to users of specific groups.
 */
export class UserTarget {
    /** user id */
    id      : string;
    /** list of **groups** assigned to a user */
    groups  : string[];
}

