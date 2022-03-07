// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbContainers } from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbMessages }   from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbSchema }     from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { DbStats }      from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostDetails }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { HostCluster }  from "./Friflo.Json.Fliox.Hub.DB.Cluster";
import { int32 }        from "./Standard";
import { Change }       from "./Friflo.Json.Fliox.Hub.Protocol.Tasks";

/**
 * MonitorStore expose access information of the Hub and its databases:
 * - request and task count executed per user 
 * - request and task count executed per client. A user can access without, one or multiple client ids. 
 * - events sent to (or buffered for) clients subscribed by these clients. 
 * - aggregated access counts of the Hub in the last 30 seconds and 30 minutes.
 */
// schema documentation only - not implemented right now
export interface MonitorStore {
    // --- containers
    hosts      : { [key: string]: HostHits };
    users      : { [key: string]: UserHits };
    clients    : { [key: string]: ClientHits };
    histories  : { [key: string]: HistoryHits };

    // --- commands
    ["ClearStats"]         (param: ClearStats | null) : ClearStatsResult;
    ["std.Echo"]           (param: any) : any;
    ["std.Containers"]     () : DbContainers;
    ["std.Messages"]       () : DbMessages;
    ["std.Schema"]         () : DbSchema;
    ["std.Stats"]          (param: string | null) : DbStats;
    ["std.Details"]        () : HostDetails;
    ["std.Cluster"]        () : HostCluster;
}

export class HostHits {
    id      : string;
    counts  : RequestCount;
}

export class UserHits {
    id       : string;
    clients  : string[];
    counts?  : RequestCount[] | null;
}

export class ClientHits {
    id      : string;
    user    : string;
    counts? : RequestCount[] | null;
    event?  : EventDelivery | null;
}

export class HistoryHits {
    id          : int32;
    counters    : int32[];
    lastUpdate  : int32;
}

export class RequestCount {
    db?       : string | null;
    requests  : int32;
    tasks     : int32;
}

export class EventDelivery {
    seq          : int32;
    queued       : int32;
    messageSubs? : string[] | null;
    changeSubs?  : ChangeSubscriptions[] | null;
}

export class ChangeSubscriptions {
    container  : string;
    changes    : Change[];
    filter?    : string | null;
}

export class ClearStats {
}

export class ClearStatsResult {
}

