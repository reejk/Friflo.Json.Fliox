// Generated by: https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox/Schema
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using UserStore2.Hub.Protocol.Tasks;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.Hub.Host.Auth.Rights {

[Fri.Discriminator("type")]
[Fri.Polymorph(typeof(RightAllow),            Discriminant = "allow")]
[Fri.Polymorph(typeof(RightTask),             Discriminant = "task")]
[Fri.Polymorph(typeof(RightMessage),          Discriminant = "message")]
[Fri.Polymorph(typeof(RightSubscribeMessage), Discriminant = "subscribeMessage")]
[Fri.Polymorph(typeof(RightOperation),        Discriminant = "operation")]
[Fri.Polymorph(typeof(RightPredicate),        Discriminant = "predicate")]
public abstract class Right {
    string  description;
}

public class RightAllow : Right {
    string  database;
}

public class RightTask : Right {
    string          database;
    [Fri.Required]
    List<TaskType>  types;
}

public class RightMessage : Right {
    string        database;
    [Fri.Required]
    List<string>  names;
}

public class RightSubscribeMessage : Right {
    string        database;
    [Fri.Required]
    List<string>  names;
}

public class RightOperation : Right {
    string                               database;
    [Fri.Required]
    Dictionary<string, ContainerAccess>  containers;
}

public class ContainerAccess {
    List<OperationType>  operations;
    List<Change>         subscribeChanges;
}

public enum OperationType {
    create,
    upsert,
    delete,
    deleteAll,
    patch,
    read,
    query,
    aggregate,
    mutate,
    full,
}

public class RightPredicate : Right {
    [Fri.Required]
    List<string>  names;
}

}

