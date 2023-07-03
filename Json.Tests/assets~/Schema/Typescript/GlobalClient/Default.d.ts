// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int32 }             from "./Standard";
import { DbContainers }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }        from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }          from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }           from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { TransactionResult } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostParam }         from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostInfo }          from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserParam }         from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { UserResult }        from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { ClientParam }       from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { ClientResult }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { int64 }             from "./Standard";
import { DateTime }          from "./Standard";

// schema documentation only - not implemented right now
export interface GlobalClient {
    // --- containers
    jobs  : { [key: string]: GlobalJob };

    // --- commands
    /** Delete all jobs marked as completed / not completed */
    ["ClearCompletedJobs"]          (param: boolean) : int32;
    /** Echos the given parameter to assure the database is working appropriately. */
    ["std.Echo"]                    (param: any) : any;
    /** A command that completes after a specified number of milliseconds. */
    ["std.Delay"]                   (param: int32) : int32;
    /** List all database containers */
    ["std.Containers"]              () : DbContainers;
    /** List all database commands and messages */
    ["std.Messages"]                () : DbMessages;
    /** Return the Schema assigned to the database */
    ["std.Schema"]                  () : DbSchema;
    /** Return the number of entities of all containers (or the given container) of the database */
    ["std.Stats"]                   (param: string | null) : DbStats;
    /**
     * Begin a transaction containing all subsequent **SyncTask**'s.  
     * The transaction ends by either calling **SyncTasks** or explicit by
     * **TransactionCommit** / **TransactionRollback**
     */
    ["std.TransactionBegin"]        () : TransactionResult;
    /** Commit a transaction started previously with **TransactionBegin** */
    ["std.TransactionCommit"]       () : TransactionResult;
    /** Rollback a transaction started previously with **TransactionBegin** */
    ["std.TransactionRollback"]     () : TransactionResult;
    /** Returns general information about the Hub like version, host, project and environment name */
    ["std.Host"]                    (param: HostParam | null) : HostInfo;
    /** List all databases and their containers hosted by the Hub */
    ["std.Cluster"]                 () : HostCluster;
    /** Return the groups of the current user. Optionally change the groups of the current user */
    ["std.User"]                    (param: UserParam | null) : UserResult;
    /** Return client specific infos and adjust general client behavior like **queueEvents** */
    ["std.Client"]                  (param: ClientParam | null) : ClientResult;
}

export class GlobalJob {
    id           : int64;
    /** short job title / name */
    title        : string;
    completed?   : boolean | null;
    created?     : DateTime | null;
    description? : string | null;
}
