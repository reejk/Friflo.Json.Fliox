// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package Fliox.DB.Protocol

import kotlinx.serialization.*
import CustomSerializer.*
import kotlinx.serialization.json.*
import java.util.*
import Fliox.Transform.*

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolMessage  {
}

@Serializable
@SerialName("sync")
data class SyncRequest (
    override  val req   : Int? = null,
              val user  : String? = null,
              val token : String? = null,
              val ack   : Int? = null,
              val tasks : List<SyncRequestTask>,
              val info  : JsonElement? = null,
) : ProtocolRequest()

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolRequest  : ProtocolMessage() {
    abstract  val req : Int?
}

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
data class ReadEntitiesList (
              val container : String,
              val keyName   : String? = null,
              val isIntKey  : Boolean? = null,
              val reads     : List<ReadEntities>,
    override  val info      : JsonElement? = null,
) : SyncRequestTask()

@Serializable
data class ReadEntities (
              val ids        : List<String>,
              val references : List<References>? = null,
)

@Serializable
data class References (
              val selector   : String,
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val references : List<References>? = null,
)

@Serializable
@SerialName("query")
data class QueryEntities (
              val container  : String,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val filterLinq : String? = null,
              val filter     : FilterOperation? = null,
              val references : List<References>? = null,
    override  val info       : JsonElement? = null,
) : SyncRequestTask()

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
              val name  : String,
              val value : JsonElement,
    override  val info  : JsonElement? = null,
) : SyncRequestTask()

@Serializable
@SerialName("subscribeChanges")
data class SubscribeChanges (
              val container : String,
              val changes   : List<Change>,
              val filter    : FilterOperation? = null,
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
@SerialName("syncResp")
data class SyncResponse (
    override  val req          : Int? = null,
              val tasks        : List<SyncTaskResult>? = null,
              val results      : List<ContainerEntities>? = null,
              val createErrors : HashMap<String, EntityErrors>? = null,
              val upsertErrors : HashMap<String, EntityErrors>? = null,
              val patchErrors  : HashMap<String, EntityErrors>? = null,
              val deleteErrors : HashMap<String, EntityErrors>? = null,
              val info         : JsonElement? = null,
) : ProtocolResponse()

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolResponse  : ProtocolMessage() {
    abstract  val req : Int?
}

@Serializable
// @JsonClassDiscriminator("task") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class SyncTaskResult  {
}

@Serializable
@SerialName("create")
data class CreateEntitiesResult (
              val Error : CommandError? = null,
) : SyncTaskResult()

@Serializable
data class CommandError (
              val message : String? = null,
)

@Serializable
@SerialName("upsert")
data class UpsertEntitiesResult (
              val Error : CommandError? = null,
) : SyncTaskResult()

@Serializable
@SerialName("read")
data class ReadEntitiesListResult (
              val reads : List<ReadEntitiesResult>,
) : SyncTaskResult()

@Serializable
data class ReadEntitiesResult (
              val Error      : CommandError? = null,
              val references : List<ReferencesResult>? = null,
)

@Serializable
data class ReferencesResult (
              val error      : String? = null,
              val container  : String? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
)

@Serializable
@SerialName("query")
data class QueryEntitiesResult (
              val Error      : CommandError? = null,
              val container  : String? = null,
              val filterLinq : String? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
) : SyncTaskResult()

@Serializable
@SerialName("patch")
data class PatchEntitiesResult (
              val Error : CommandError? = null,
) : SyncTaskResult()

@Serializable
@SerialName("delete")
data class DeleteEntitiesResult (
              val Error : CommandError? = null,
) : SyncTaskResult()

@Serializable
@SerialName("message")
data class SendMessageResult (
              val Error  : CommandError? = null,
              val result : JsonElement? = null,
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
              val Error : CommandError? = null,
              val keys  : ReservedKeys? = null,
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
    InvalidTask,
    PermissionDenied,
    SyncError,
}

@Serializable
data class ContainerEntities (
              val container : String,
              val entities  : List<JsonElement>,
              val notFound  : List<String>? = null,
              val errors    : HashMap<String, EntityError>? = null,
)

@Serializable
data class EntityError (
              val type    : EntityErrorType,
              val message : String? = null,
)

enum class EntityErrorType {
    Undefined,
    ParseError,
    ReadError,
    WriteError,
    DeleteError,
    PatchError,
}

@Serializable
data class EntityErrors (
              val container : String? = null,
              val errors    : HashMap<String, EntityError>,
)

@Serializable
@SerialName("error")
data class ErrorResponse (
    override  val req     : Int? = null,
              val message : String? = null,
) : ProtocolResponse()

@Serializable
@SerialName("sub")
data class SubscriptionEvent (
    override  val seq   : Int,
    override  val src   : String,
    override  val dst   : String,
              val tasks : List<SyncRequestTask>? = null,
) : ProtocolEvent()

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolEvent  : ProtocolMessage() {
    abstract  val seq : Int
    abstract  val src : String
    abstract  val dst : String
}

