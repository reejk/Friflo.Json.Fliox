// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
package UnitTest.Fliox.Client

import kotlinx.serialization.*
import CustomSerializer.*
import kotlinx.datetime.*
import java.math.*
import kotlinx.serialization.json.*
import Standard.*

@Serializable
abstract class PocStore {
    abstract  val orders      : HashMap<String, Order>
    abstract  val customers   : HashMap<String, Customer>
    abstract  val articles    : HashMap<String, Article>
    abstract  val articles2   : HashMap<String, Article>
    abstract  val producers   : HashMap<String, Producer>
    abstract  val employees   : HashMap<String, Employee>
    abstract  val types       : HashMap<String, TestType>
    abstract  val nonClsTypes : HashMap<String, NonClsType>
    abstract  val keyName     : HashMap<String, TestKeyName>
}

@Serializable
data class Order (
              val id       : String,
              val customer : String? = null,
              val created  : Instant,
              val items    : List<OrderItem>? = null,
)

@Serializable
data class Customer (
              val id   : String,
              val name : String,
)

@Serializable
data class Article (
              val id       : String,
              val name     : String,
              val producer : String? = null,
)

@Serializable
data class Producer (
              val id        : String,
              val name      : String,
              val employees : List<String>? = null,
)

@Serializable
data class Employee (
              val id        : String,
              val firstName : String,
              val lastName  : String? = null,
)

@Serializable
data class TestType (
    override  val id               : String,
              val dateTime         : Instant,
              val dateTimeNull     : Instant? = null,
              @Serializable(with = BigIntegerSerializer::class)
              val bigInt           : BigInteger,
              @Serializable(with = BigIntegerSerializer::class)
              val bigIntNull       : BigInteger? = null,
              val boolean          : Boolean,
              val booleanNull      : Boolean? = null,
              val uint8            : Byte,
              val uint8Null        : Byte? = null,
              val int16            : Short,
              val int16Null        : Short? = null,
              val int32            : Int,
              val int32Null        : Int? = null,
              val int64            : Long,
              val int64Null        : Long? = null,
              val float32          : Float,
              val float32Null      : Float? = null,
              val float64          : Double,
              val float64Null      : Double? = null,
              val pocStruct        : PocStruct,
              val pocStructNull    : PocStruct? = null,
              val intArray         : List<Int>,
              val intArrayNull     : List<Int>? = null,
              val intNullArray     : List<Int?>? = null,
              val jsonValue        : JsonElement? = null,
              val derivedClass     : DerivedClass,
              val derivedClassNull : DerivedClass? = null,
              val testEnum         : TestEnum,
              val testEnumNull     : TestEnum? = null,
) : PocEntity()

@Serializable
data class NonClsType (
              val id         : String,
              val int8       : int8,
              val uint16     : uint16,
              val uint32     : uint32,
              val uint64     : uint64,
              val int8Null   : int8? = null,
              val uint16Null : uint16? = null,
              val uint32Null : uint32? = null,
              val uint64Null : uint64? = null,
)

@Serializable
data class TestKeyName (
              val testId : String,
              val value  : String? = null,
)

@Serializable
data class OrderItem (
              val article : String,
              val amount  : Int,
              val name    : String? = null,
)

@Serializable
abstract class PocEntity {
    abstract  val id : String
}

@Serializable
data class PocStruct (
              val value : Int,
)

@Serializable
data class DerivedClass (
              val article    : String,
              val amount     : Int,
              val name       : String? = null,
              val derivedVal : Int,
)

enum class TestEnum {
    NONE,
    e1,
    e2,
}

@Serializable
data class TestCommand (
              val text : String? = null,
)

