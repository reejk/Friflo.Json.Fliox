// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema

export abstract class ClusterStore {
    catalogs  : { [key: string]: Catalog };
    schemas   : { [key: string]: CatalogSchema };
}

export interface ClusterStoreService {
    Echo (command: any) : any;
}

export class Catalog {
    id            : string;
    databaseType  : string;
    containers    : string[];
}

export class CatalogSchema {
    id          : string;
    schemaName  : string;
    schemaPath  : string;
    schemas     : { [key: string]: string };
}

