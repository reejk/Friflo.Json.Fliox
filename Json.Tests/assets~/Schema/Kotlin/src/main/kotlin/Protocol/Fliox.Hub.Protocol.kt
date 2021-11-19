// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package Fliox.Hub.Protocol

import kotlinx.serialization.*
import CustomSerializer.*
import Fliox.Hub.Protocol.Tasks.*
import kotlinx.serialization.json.*
import Fliox.Hub.Protocol.Models.*

@Serializable
// @JsonClassDiscriminator("msg") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolMessage  {
}

@Serializable
@SerialName("sync")
data class SyncRequest (
    override  val req      : Int? = null,
    override  val clt      : String? = null,
              val user     : String? = null,
              val token    : String? = null,
              val ack      : Int? = null,
              val tasks    : List<SyncRequestTask>,
              val database : String? = null,
              val info     : JsonElement? = null,
) : ProtocolRequest()

@Serializable
// @JsonClassDiscriminator("msg") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolRequest  : ProtocolMessage() {
    abstract  val req : Int?
    abstract  val clt : String?
}

@Serializable
@SerialName("resp")
data class SyncResponse (
    override  val req          : Int? = null,
    override  val clt          : String? = null,
              val database     : String? = null,
              val tasks        : List<SyncTaskResult>? = null,
              val containers   : List<ContainerEntities>? = null,
              val createErrors : HashMap<String, EntityErrors>? = null,
              val upsertErrors : HashMap<String, EntityErrors>? = null,
              val patchErrors  : HashMap<String, EntityErrors>? = null,
              val deleteErrors : HashMap<String, EntityErrors>? = null,
              val info         : JsonElement? = null,
) : ProtocolResponse()

@Serializable
// @JsonClassDiscriminator("msg") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolResponse  : ProtocolMessage() {
    abstract  val req : Int?
    abstract  val clt : String?
}

@Serializable
data class ContainerEntities (
              val container : String,
              val entities  : List<JsonElement>,
              val notFound  : List<String>? = null,
              val errors    : HashMap<String, EntityError>? = null,
)

@Serializable
data class EntityErrors (
              val container : String? = null,
              val errors    : HashMap<String, EntityError>,
)

@Serializable
@SerialName("error")
data class ErrorResponse (
    override  val req     : Int? = null,
    override  val clt     : String? = null,
              val message : String? = null,
              val type    : ErrorResponseType,
) : ProtocolResponse()

enum class ErrorResponseType {
    Internal,
    BadRequest,
    BadResponse,
}

@Serializable
@SerialName("ev")
data class EventMessage (
    override  val seq   : Int,
    override  val src   : String,
    override  val clt   : String,
              val tasks : List<SyncRequestTask>? = null,
) : ProtocolEvent()

@Serializable
// @JsonClassDiscriminator("msg") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class ProtocolEvent  : ProtocolMessage() {
    abstract  val seq : Int
    abstract  val src : String
    abstract  val clt : String
}

