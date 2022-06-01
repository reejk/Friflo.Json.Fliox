// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

#pragma warning disable 0169 // [CS0169] The field '...' is never used

namespace EntityIdStore2.Cluster {

public class DbContainers {
    [RequiredMember]
    string        id;
    [RequiredMember]
    string        storage;
    [RequiredMember]
    List<string>  containers;
}

public class DbMessages {
    [RequiredMember]
    string        id;
    [RequiredMember]
    List<string>  commands;
    [RequiredMember]
    List<string>  messages;
}

public class DbSchema {
    [RequiredMember]
    string                         id;
    [RequiredMember]
    string                         schemaName;
    [RequiredMember]
    string                         schemaPath;
    [RequiredMember]
    Dictionary<string, JsonValue>  jsonSchemas;
}

public class DbStats {
    List<ContainerStats>  containers;
}

public class ContainerStats {
    [RequiredMember]
    string  name;
    long    count;
}

public class HostDetails {
    [RequiredMember]
    string        version;
    string        hostName;
    string        projectName;
    string        projectWebsite;
    string        envName;
    string        envColor;
    [RequiredMember]
    List<string>  routes;
}

public class HostCluster {
    [RequiredMember]
    List<DbContainers>  databases;
}

}

