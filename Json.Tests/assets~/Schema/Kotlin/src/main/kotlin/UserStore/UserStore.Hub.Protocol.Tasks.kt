// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package UserStore.Hub.Protocol.Tasks

import kotlinx.serialization.*
import CustomSerializer.*

enum class TaskType {
    read,
    query,
    create,
    upsert,
    patch,
    delete,
    aggregate,
    message,
    command,
    closeCursors,
    subscribeChanges,
    subscribeMessage,
    reserveKeys,
    error,
}

enum class EntityChange {
    create,
    upsert,
    patch,
    delete,
}

