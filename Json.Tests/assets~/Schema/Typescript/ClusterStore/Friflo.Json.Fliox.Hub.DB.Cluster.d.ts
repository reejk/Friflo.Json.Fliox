// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
import { int64 } from "./Standard";

/**
 * ClusterStore provide information about databases hosted by the Hub: 
 * - available containers aka tables per database 
 * - available commands per database 
 * - the schema assigned to each database
 */
// schema documentation only - not implemented right now
export interface ClusterStore {
    // --- containers
    containers  : { [key: string]: DbContainers };
    messages    : { [key: string]: DbMessages };
    schemas     : { [key: string]: DbSchema };

    // --- commands
    ["std.Echo"]           (param: any) : any;
    ["std.Containers"]     () : DbContainers;
    ["std.Messages"]       () : DbMessages;
    ["std.Schema"]         () : DbSchema;
    ["std.Stats"]          (param: string | null) : DbStats;
    ["std.Details"]        () : HostDetails;
    ["std.Cluster"]        () : HostCluster;
}

export class DbContainers {
    id          : string;
    storage     : string;
    containers  : string[];
}

export class DbMessages {
    id        : string;
    commands  : string[];
    messages  : string[];
}

export class DbSchema {
    id           : string;
    schemaName   : string;
    schemaPath   : string;
    jsonSchemas  : { [key: string]: any };
}

export class DbStats {
    containers? : ContainerStats[] | null;
}

export class ContainerStats {
    name   : string;
    count  : int64;
}

export class HostDetails {
    version         : string;
    hostName?       : string | null;
    projectName?    : string | null;
    projectWebsite? : string | null;
    envName?        : string | null;
    /**
     * the color used to display the environment name in GUI's using CSS color format.
     * E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
     */
    envColor?       : string | null;
}

export class HostCluster {
    databases  : DbContainers[];
}

