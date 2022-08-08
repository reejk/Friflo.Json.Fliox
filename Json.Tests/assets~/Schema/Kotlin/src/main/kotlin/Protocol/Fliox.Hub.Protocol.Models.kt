// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package Fliox.Hub.Protocol.Models

import kotlinx.serialization.*
import CustomSerializer.*

@Serializable
data class References (
              val selector   : String,
              val container  : String,
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
              val container  : String? = null,
              val count      : Int? = null,
              val ids        : List<String>,
              val references : List<ReferencesResult>? = null,
)

