// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
package UserStore.DB.Auth.Rights

import kotlinx.serialization.*
import CustomSerializer.*
import UserStore.DB.Sync.*

@Serializable
// @JsonClassDiscriminator("type") https://github.com/Kotlin/kotlinx.serialization/issues/546
abstract class Right  {
    abstract  val description : String?
}

@Serializable
@SerialName("allow")
data class RightAllow (
              val grant       : Boolean,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("task")
data class RightTask (
              val types       : List<TaskType>,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("message")
data class RightMessage (
              val names       : List<String>,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("subscribeMessage")
data class RightSubscribeMessage (
              val names       : List<String>,
    override  val description : String? = null,
) : Right()

@Serializable
@SerialName("database")
data class RightDatabase (
              val containers  : HashMap<String, ContainerAccess>,
    override  val description : String? = null,
) : Right()

@Serializable
data class ContainerAccess (
              val operations       : List<OperationType>? = null,
              val subscribeChanges : List<Change>? = null,
)

enum class OperationType {
    create,
    upsert,
    delete,
    patch,
    read,
    query,
    mutate,
    full,
}

@Serializable
@SerialName("predicate")
data class RightPredicate (
              val names       : List<String>,
    override  val description : String? = null,
) : Right()

