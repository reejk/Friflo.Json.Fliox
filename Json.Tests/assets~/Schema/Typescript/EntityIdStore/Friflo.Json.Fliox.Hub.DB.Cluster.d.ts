// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int64 } from "./Standard";
import { int32 } from "./Standard";

/** **containers** and **storage** type of a database */
export class DbContainers {
    /** database name */
    id          : string;
    /** **storage** type. e.g. memory, file-system, ... */
    storage     : string;
    /** list of database **containers** */
    containers  : string[];
}

/** **commands** and **messages** of a database */
export class DbMessages {
    /** database name */
    id        : string;
    /** list of database **commands** */
    commands  : string[];
    /** list of database **messages** */
    messages  : string[];
}

/**
 * A **DbSchema** can be assigned to a database to specify its **containers**, **commands** and **messages**.  
 * The types used by the Schema are declared within **jsonSchemas**.  
 * The type referenced by the tuple **schemaName** / **schemaPath** specify the
 * database containers, commands and messages.
 */
export class DbSchema {
    /** database name */
    id           : string;
    /** refer a type definition of the JSON Schema referenced with **schemaPath** */
    schemaName   : string;
    /** refer a JSON Schema in **jsonSchemas** */
    schemaPath   : string;
    /**
     * map of **JSON Schemas** each containing a set of type definitions.  
     * Each JSON Schema is identified by its unique path
     */
    jsonSchemas  : { [key: string]: any };
}

/** list of container statistics. E.g. the number of entities per container */
export class DbStats {
    /** list of container statistics - number of entities per container */
    containers? : ContainerStats[] | null;
}

/** statistics of a single container. E.g. the number of entities in a container */
export class ContainerStats {
    /** container name */
    name   : string;
    /** number of entities / records within a container */
    count  : int64;
}

export class HostParam {
    memory?    : boolean | null;
    gcCollect? : boolean | null;
}

/** general information about a Hub */
export class HostInfo {
    /** host version */
    hostVersion     : string;
    /** Fliox library version */
    flioxVersion    : string;
    /**
     * host name. Used as **id** in
     * **hosts** of database **monitor**
     */
    hostName?       : string | null;
    /** project name */
    projectName?    : string | null;
    /** link to a website describing project and Hub */
    projectWebsite? : string | null;
    /** environment name. e.g. 'dev', 'test', 'staging', 'prod' */
    envName?        : string | null;
    /**
     * the color used to display the environment name in GUI's using CSS color format.  
     * E.g. using red for a production environment: "#ff0000" or "rgb(255 0 0)"
     */
    envColor?       : string | null;
    /** is true if host support Pub-Sub. */
    pubSub          : boolean;
    /** routes configures by **HttpHost** - commonly below /fliox */
    routes          : string[];
    memory?         : HostMemory | null;
}

export class HostMemory {
    totalAllocatedBytes  : int64;
    totalMemory          : int64;
    gc?                  : HostGCMemory | null;
}

/** **GCMemoryInfo** */
export class HostGCMemory {
    highMemoryLoadThresholdBytes  : int64;
    totalAvailableMemoryBytes     : int64;
    memoryLoadBytes               : int64;
    heapSizeBytes                 : int64;
    fragmentedBytes               : int64;
}

/** All **databases** hosted by Hub */
export class HostCluster {
    /** list of **databases** hosted by Hub */
    databases  : DbContainers[];
}

export class UserParam {
    addGroups?    : string[] | null;
    removeGroups? : string[] | null;
}

export class UserResult {
    groups  : string[];
}

export class ClientParam {
    /** Return the client id set in **SyncRequest** or creates a new one in case is was not set. */
    ensureClientId? : boolean | null;
    /**
     * If **false** the hub try to send events to a client when the events are emitted.
     * Sending events to a disconnected client will never arrive.   
     * If **true** the hub will store all unacknowledged events for a client in a FIFO queue and send them on reconnects.
     */
    queueEvents?    : boolean | null;
}

export class ClientResult {
    /**
     * return number of queued events not acknowledged by the client.
     * Events are queued only if the client instruct the Hub to queue events by setting **queueEvents** = true
     */
    queuedEvents  : int32;
    /**
     * return the client id set in the **SyncRequest**. Can be null.  
     * A new client id is created in case any task requires a client id and the **SyncRequest** did not set a client id.  
     * E.g. **ensureClientId** = true or **queueEvents** = true
     */
    clientId?     : string | null;
}

