// Generated by: https://github.com/friflo/Friflo.Json.Flow/tree/main/Json/Flow/Schema
using System;
using System.Collections.Generic;
using System.Numerics;
using Flow.Sync;

#pragma warning disable 0169

namespace Flow.Auth.Rights {

public  abstract class Right {
    string description;
}

public class RightAllow : Right {
    bool grant;
}

public class RightTask : Right {
    List<TaskType> types;
}

public class RightMessage : Right {
    List<string> names;
}

public class RightSubscribeMessage : Right {
    List<string> names;
}

public class RightDatabase : Right {
    Dictionary<string, ContainerAccess> containers;
}

public class ContainerAccess {
    List<OperationType> operations;
    List<Change>        subscribeChanges;
}

public enum OperationType {
    create,
    update,
    delete,
    patch,
    read,
    query,
    mutate,
    full,
}

public class RightPredicate : Right {
    List<string> names;
}

}

