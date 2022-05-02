// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
import { int64 } from "./Standard";

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

/** general information about a Hub */
export class HostDetails {
    /** host version */
    version         : string;
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
     * E.g. using red for a production environment: '#ff0000' or 'rgb(255 0 0)'
     */
    envColor?       : string | null;
    /** routes configures by **HttpHost** - commonly below /fliox */
    routes          : string[];
}

/** All **databases** hosted by Hub */
export class HostCluster {
    /** list of **databases** hosted by Hub */
    databases  : DbContainers[];
}

