// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { DbInfo }                from "./Friflo.Json.Fliox.Hub.DB.Cluster"
import { DbSchema }              from "./Friflo.Json.Fliox.Hub.DB.Cluster"
import { DbList }                from "./Friflo.Json.Fliox.Hub.DB.Cluster"
import { RequestCount }          from "./Friflo.Json.Fliox.Hub.Host.Stats"
import { int32 }                 from "./Standard"
import { Change }                from "./Friflo.Json.Fliox.Hub.Protocol.Tasks"
import { FilterOperation }       from "./Friflo.Json.Fliox.Transform"
import { FilterOperation_Union } from "./Friflo.Json.Fliox.Transform"

export abstract class MonitorStore {
    hosts      : { [key: string]: HostInfo };
    clients    : { [key: string]: ClientInfo };
    users      : { [key: string]: UserInfo };
    histories  : { [key: string]: HistoryInfo };
}

export interface MonitorStoreService {
    ClearStats (value: ClearStats) : ClearStatsResult;
    DbInfo     (value: any) : DbInfo;
    DbSchema   (value: any) : DbSchema;
    DbList     (value: any) : DbList;
    Echo       (value: any) : any;
}

export class HostInfo {
    id      : string;
    counts  : RequestCount;
}

export class ClientInfo {
    id      : string;
    user    : string;
    counts? : RequestCount[] | null;
    event?  : EventInfo | null;
}

export class UserInfo {
    id       : string;
    clients  : string[];
    counts?  : RequestCount[] | null;
}

export class EventInfo {
    seq          : int32;
    queued       : int32;
    messageSubs? : string[] | null;
    changeSubs?  : ChangeSubscriptions[] | null;
}

export class ChangeSubscriptions {
    container  : string;
    changes    : Change[];
    filter?    : FilterOperation_Union | null;
}

export class HistoryInfo {
    id          : int32;
    counters    : int32[];
    lastUpdate  : int32;
}

export class ClearStats {
}

export class ClearStatsResult {
}

