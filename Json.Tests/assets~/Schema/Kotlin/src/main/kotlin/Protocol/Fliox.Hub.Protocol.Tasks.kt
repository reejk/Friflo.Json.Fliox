// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package Fliox.Hub.Protocol.Tasks

import kotlinx.serialization.*
import CustomSerializer.*
import kotlinx.serialization.json.*
import java.util.*
import Fliox.Hub.Protocol.Models.*
import Fliox.Transform.*

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class SyncRequestTask  {
    abstract  val info : JsonElement?
}

@Serializable
@SerialName("create")
data class CreateEntities (
              val container     : String,
              @Serializable(with = UUIDSerializer::class)
              val reservedToken : UUID? = null,
              val keyName       : String? = null,
              val entities      : List<JsonElement>,
    override  val info          : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("upsert")
data class UpsertEntities (
              val container : String,
              val keyName   : String? = null,
              val entities  : List<JsonElement>,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("read")
data class ReadEntities (
              val container : String,
              val keyName   : String? = null,
              val isIntKey  : Boolean? = null,
              val sets      : List<ReadEntitiesSet>,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("query")
data class QueryEntities (
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val filterTree : JsonElement? = null,
              val filter     : String? = null,
              val references : List<References>? = null,
              val limit      : Int? = null,
              val maxCount   : Int? = null,
              val cursor     : String? = null,
    override  val info       : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("aggregate")
data class AggregateEntities (
              val container  : String,
              val type       : AggregateType,
              val filterTree : JsonElement? = null,
              val filter     : String? = null,
    override  val info       : JsonElement? = null,
) : SyncRequestTask()

enum class AggregateType {
    count,
}

@Serializable
@SerialName("patch")
data class PatchEntities (
              val container : String,
              val keyName   : String? = null,
              val patches   : HashMap<String, EntityPatch>,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
data class EntityPatch (
              val patches : List<JsonPatch>,
)

@Serializable
@SerialName("delete")
data class DeleteEntities (
              val container : String,
              val ids       : List<String>? = null,
              val all       : Boolean? = null,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("message")
data class SendMessage (
    override  val name  : String,
    override  val param : JsonElement? = null,
    override  val info  : JsonElement? = null,
) : SyncMessageTask()

@Serializable
abstract class SyncMessageTask {
    abstract  val name  : String
    abstract  val param : JsonElement?
    abstract  val info  : JsonElement?
}

@Serializable
@SerialName("command")
data class SendCommand (
    override  val name  : String,
    override  val param : JsonElement? = null,
    override  val info  : JsonElement? = null,
) : SyncMessageTask()

@Serializable
@SerialName("closeCursors")
data class CloseCursors (
              val container : String,
              val cursors   : List<String>? = null,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("subscribeChanges")
data class SubscribeChanges (
              val container : String,
              val changes   : List<Change>,
              val filter    : JsonElement? = null,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

enum class Change {
    create,
    upsert,
    patch,
    delete,
}

@Serializable
@SerialName("subscribeMessage")
data class SubscribeMessage (
              val name   : String,
              val remove : Boolean? = null,
    override  val info   : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("reserveKeys")
data class ReserveKeys (
              val container : String,
              val count     : Int,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class SyncTaskResult  {
}

@Serializable
@SerialName("create")
data class CreateEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("upsert")
data class UpsertEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("read")
data class ReadEntitiesResult (
              val sets : List<ReadEntitiesSetResult>,
) : SyncTaskResult()

@Serializable
@SerialName("query")
data class QueryEntitiesResult (
              val container  : String? = null,
              val cursor     : String? = null,
              val count      : Int? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("aggregate")
data class AggregateEntitiesResult (
              val container : String? = null,
              val value     : Double? = null,
) : SyncTaskResult()

@Serializable
@SerialName("patch")
data class PatchEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("delete")
data class DeleteEntitiesResult (
              val errors : List<EntityError>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("message")
class SendMessageResult (
) : SyncMessageResult()

@Serializable
abstract class SyncMessageResult {
}

@Serializable
@SerialName("command")
data class SendCommandResult (
              val result : JsonElement? = null,
) : SyncMessageResult()

@Serializable
@SerialName("closeCursors")
data class CloseCursorsResult (
              val count : Int,
) : SyncTaskResult()

@Serializable
@SerialName("subscribeChanges")
class SubscribeChangesResult (
) : SyncTaskResult()

@Serializable
@SerialName("subscribeMessage")
class SubscribeMessageResult (
) : SyncTaskResult()

@Serializable
@SerialName("reserveKeys")
data class ReserveKeysResult (
              val keys : ReservedKeys? = null,
) : SyncTaskResult()

@Serializable
data class ReservedKeys (
              val start : Long,
              val count : Int,
              @Serializable(with = UUIDSerializer::class)
              val token : UUID,
)

@Serializable
@SerialName("error")
data class TaskErrorResult (
              val type       : TaskErrorResultType,
              val message    : String? = null,
              val stacktrace : String? = null,
) : SyncTaskResult()

enum class TaskErrorResultType {
    None,
    UnhandledException,
    DatabaseError,
    FilterError,
    ValidationError,
    CommandError,
    InvalidTask,
    NotImplemented,
    PermissionDenied,
    SyncError,
}

