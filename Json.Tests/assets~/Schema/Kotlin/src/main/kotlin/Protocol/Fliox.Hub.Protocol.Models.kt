// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package Fliox.Hub.Protocol.Models

import kotlinx.serialization.*
import CustomSerializer.*
import Fliox.Hub.Protocol.Tasks.*
import kotlinx.serialization.json.*

@Serializable
data class References (
              val selector   : String,
              val cont       : String,
              val orderByKey : Order? = null,
              val keyName    : String? = null,
              val isIntKey   : Boolean? = null,
              val references : List<References>? = null,
)

@Serializable
data class EntityError (
              val id      : String,
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
data class ReferencesResult (
              val error      : String? = null,
              val cont       : String? = null,
              val len        : Int? = null,
              val set        : List<JsonElement>,
              val references : List<ReferencesResult>? = null,
)

