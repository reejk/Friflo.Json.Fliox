// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;
using EntityIdStore2.Hub.Protocol.Tasks;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace EntityIdStore2.Hub.DB.Cluster {

public class DbContainers {
    [Required]
    string        id;
    [Required]
    string        storage;
    [Required]
    List<string>  containers;
}

public class DbMessages {
    [Required]
    string        id;
    [Required]
    List<string>  commands;
    [Required]
    List<string>  messages;
}

public class DbSchema {
    [Required]
    string                         id;
    [Required]
    string                         schemaName;
    [Required]
    string                         schemaPath;
    [Required]
    Dictionary<string, JsonValue>  jsonSchemas;
}

public class DbStats {
    List<ContainerStats>  containers;
}

public class ContainerStats {
    [Required]
    string  name;
    long    count;
}

public class HostParam {
    bool? memory;
    bool? gcCollect;
}

public class HostInfo {
    [Required]
    string        hostVersion;
    [Required]
    string        flioxVersion;
    string        hostName;
    string        projectName;
    string        projectWebsite;
    string        envName;
    string        envColor;
    bool          pubSub;
    [Required]
    List<string>  routes;
    HostMemory    memory;
}

public class HostMemory {
    long          totalAllocatedBytes;
    long          totalMemory;
    HostGCMemory  gc;
}

public class HostGCMemory {
    long  highMemoryLoadThresholdBytes;
    long  totalAvailableMemoryBytes;
    long  memoryLoadBytes;
    long  heapSizeBytes;
    long  fragmentedBytes;
}

public class HostCluster {
    [Required]
    List<DbContainers>  databases;
}

public class UserParam {
    List<string>  addGroups;
    List<string>  removeGroups;
}

public class UserResult {
    [Required]
    List<string>        groups;
    [Required]
    List<string>        clients;
    [Required]
    List<RequestCount>  counts;
}

public struct RequestCount {
    string  db;
    int     requests;
    int     tasks;
}

public class ClientParam {
    bool? ensureClientId;
    bool? queueEvents;
}

public class ClientResult {
    bool                queueEvents;
    int                 queuedEvents;
    string              clientId;
    SubscriptionEvents? subscriptionEvents;
}

public struct SubscriptionEvents {
    int                       seq;
    int                       queued;
    bool                      queueEvents;
    bool                      connected;
    List<string>              messageSubs;
    List<ChangeSubscription>  changeSubs;
}

public class ChangeSubscription {
    [Required]
    string              container;
    [Required]
    List<EntityChange>  changes;
    string              filter;
}

}

