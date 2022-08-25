// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace UserStore2.Hub.DB.Cluster {

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
    [Required]
    List<string>  routes;
    [Required]
    HostMemory    memory;
}

public class HostMemory {
    long  highMemoryLoadThresholdBytes;
    long  totalAvailableMemoryBytes;
    long  memoryLoadBytes;
    long  heapSizeBytes;
    long  fragmentedBytes;
    long  totalAllocatedBytes;
    long  totalMemory;
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
    List<string>  groups;
}

public class ClientParam {
}

public class ClientResult {
    int  queuedEvents;
}

}

