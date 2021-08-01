// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
import kotlinx.serialization.*

@Serializable
data class Role (
    override  val id          : String,
              val rights      : List<Right>,
              val description : String? = null,
) : Entity()

@Serializable
data class UserCredential (
    override  val id       : String,
              val passHash : String? = null,
              val token    : String? = null,
) : Entity()

@Serializable
data class UserPermission (
    override  val id    : String,
              val roles : List<String>? = null,
) : Entity()

