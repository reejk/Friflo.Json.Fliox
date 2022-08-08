// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package Friflo.Json.Fliox.Hub.DB.Cluster

import kotlinx.serialization.*
import CustomSerializer.*
import kotlinx.serialization.json.*

@Serializable
data class DbContainers (
              val id         : String,
              val storage    : String,
              val containers : List<String>,
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
data class HostInfo (
              val hostVersion    : String,
              val flioxVersion   : String,
              val hostName       : String? = null,
              val projectName    : String? = null,
              val projectWebsite : String? = null,
              val envName        : String? = null,
              val envColor       : String? = null,
              val routes         : List<String>,
)

@Serializable
data class HostCluster (
              val databases : List<DbContainers>,
)

@Serializable
data class UserOptions (
              val addGroups    : List<String>? = null,
              val removeGroups : List<String>? = null,
)

@Serializable
data class UserResult (
              val groups : List<String>,
)

