// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema

/** Filter type used to specify the type of a database change. */
export type Change =
    | "create"      /** filter change events of created entities. */
    | "upsert"      /** filter change events of upserted entities. */
    | "patch"       /** filter change events of entity patches. */
    | "delete"      /** filter change events of deleted entities. */
;

