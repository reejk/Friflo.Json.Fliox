// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package UserStore.Hub.DB.Cluster

import kotlinx.serialization.*
import CustomSerializer.*
import kotlinx.serialization.json.*
import Standard.*

@Serializable
data class DbContainers (
              val id         : String,
              val storage    : String,
              val containers : List<String>,
              val defaultDB  : Boolean? = null,
)

@Serializable
data class DbMessages (
              val id       : String,
              val commands : List<String>,
              val messages : List<String>,
)

@Serializable
data class DbSchema (
              val id          : String,
              val schemaName  : String,
              val schemaPath  : String,
              val jsonSchemas : HashMap<String, JsonElement>,
)

@Serializable
data class DbStats (
              val containers : List<ContainerStats>? = null,
)

@Serializable
data class ContainerStats (
              val name  : String,
              val count : Long,
)

@Serializable
data class TransactionResult (
              val executed : TransactionCommand,
)

enum class TransactionCommand {
    Commit,
    Rollback,
}

@Serializable
data class RawSql (
              val command : String,
              val schema  : Boolean? = null,
)

@Serializable
data class RawSqlResult (
              val rowCount    : Int,
              val columnCount : Int,
              val columns     : List<RawSqlColumn>? = null,
              val data        : JsonTable? = null,
)

@Serializable
data class RawSqlColumn (
              val name : String? = null,
              val type : RawColumnType,
)

enum class RawColumnType {
    Unknown,
    Bool,
    Uint8,
    Int16,
    Int32,
    Int64,
    String,
    DateTime,
    Guid,
    Float,
    Double,
    JSON,
}

@Serializable
data class HostParam (
              val memory    : Boolean? = null,
              val gcCollect : Boolean? = null,
)

@Serializable
data class HostInfo (
              val hostName       : String,
              val hostVersion    : String,
              val flioxVersion   : String,
              val projectName    : String? = null,
              val projectWebsite : String? = null,
              val envName        : String? = null,
              val envColor       : String? = null,
              val pubSub         : Boolean,
              val routes         : List<String>,
              val memory         : HostMemory? = null,
)

@Serializable
data class HostMemory (
              val totalAllocatedBytes : Long,
              val totalMemory         : Long,
              val gc                  : HostGCMemory? = null,
)

@Serializable
data class HostGCMemory (
              val highMemoryLoadThresholdBytes : Long,
              val totalAvailableMemoryBytes    : Long,
              val memoryLoadBytes              : Long,
              val heapSizeBytes                : Long,
              val fragmentedBytes              : Long,
)

@Serializable
data class HostCluster (
              val databases : List<DbContainers>,
)

@Serializable
data class UserParam (
              val addGroups    : List<String>? = null,
              val removeGroups : List<String>? = null,
)

@Serializable
data class UserResult (
              val roles   : List<String>,
              val groups  : List<String>,
              val clients : List<String>,
              val counts  : List<RequestCount>,
)

@Serializable
data class RequestCount (
              val db       : String? = null,
              val requests : Int,
              val tasks    : Int,
)

@Serializable
data class ClientParam (
              val ensureClientId : Boolean? = null,
              val queueEvents    : Boolean? = null,
)

@Serializable
data class ClientResult (
              val queueEvents        : Boolean,
              val queuedEvents       : Int,
              val clientId           : String? = null,
              val subscriptionEvents : SubscriptionEvents? = null,
)

@Serializable
data class SubscriptionEvents (
              val seq         : Int,
              val queued      : Int,
              val queueEvents : Boolean,
              val connected   : Boolean,
              val endpoint    : String? = null,
              val messageSubs : List<String>? = null,
              val changeSubs  : List<ChangeSubscription>? = null,
)

@Serializable
data class ChangeSubscription (
              val container : String,
              val changes   : List<ChangeType>,
              val filter    : String? = null,
)

enum class ChangeType {
    create,
    upsert,
    merge,
    delete,
}

