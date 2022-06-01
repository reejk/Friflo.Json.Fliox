// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;
using UserStore2.Hub.Protocol.Tasks;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.Hub.Host.Auth.Rights {

[Fri.Discriminator("type")]
[Fri.Polymorph(typeof(AllowRight),            Discriminant = "allow")]
[Fri.Polymorph(typeof(TaskRight),             Discriminant = "task")]
[Fri.Polymorph(typeof(SendMessageRight),      Discriminant = "sendMessage")]
[Fri.Polymorph(typeof(SubscribeMessageRight), Discriminant = "subscribeMessage")]
[Fri.Polymorph(typeof(OperationRight),        Discriminant = "operation")]
[Fri.Polymorph(typeof(PredicateRight),        Discriminant = "predicate")]
public abstract class Right {
    string  description;
}

public class AllowRight : Right {
    [Fri.RequiredMember]
    string  database;
}

public class TaskRight : Right {
    [Fri.RequiredMember]
    string          database;
    [Fri.RequiredMember]
    List<TaskType>  types;
}

public class SendMessageRight : Right {
    [Fri.RequiredMember]
    string        database;
    [Fri.RequiredMember]
    List<string>  names;
}

public class SubscribeMessageRight : Right {
    [Fri.RequiredMember]
    string        database;
    [Fri.RequiredMember]
    List<string>  names;
}

public class OperationRight : Right {
    [Fri.RequiredMember]
    string                               database;
    [Fri.RequiredMember]
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

public class PredicateRight : Right {
    [Fri.RequiredMember]
    List<string>  names;
}

}

