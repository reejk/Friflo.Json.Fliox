// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_HostInfo
    {
        private const int Gen_hostVersion = 0;
        private const int Gen_flioxVersion = 1;
        private const int Gen_hostName = 2;
        private const int Gen_projectName = 3;
        private const int Gen_projectWebsite = 4;
        private const int Gen_envName = 5;
        private const int Gen_envColor = 6;
        private const int Gen_pubSub = 7;
        private const int Gen_routes = 8;
        private const int Gen_memory = 9;

        private static bool ReadField (ref HostInfo obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_hostVersion:    obj.hostVersion    = reader.ReadString  (field, out success);  return success;
                case Gen_flioxVersion:   obj.flioxVersion   = reader.ReadString  (field, out success);  return success;
                case Gen_hostName:       obj.hostName       = reader.ReadString  (field, out success);  return success;
                case Gen_projectName:    obj.projectName    = reader.ReadString  (field, out success);  return success;
                case Gen_projectWebsite: obj.projectWebsite = reader.ReadString  (field, out success);  return success;
                case Gen_envName:        obj.envName        = reader.ReadString  (field, out success);  return success;
                case Gen_envColor:       obj.envColor       = reader.ReadString  (field, out success);  return success;
                case Gen_pubSub:         obj.pubSub         = reader.ReadBoolean (field, out success);  return success;
                case Gen_routes:         obj.routes         = reader.ReadClass   (field, obj.routes,         out success);  return success;
                case Gen_memory:         obj.memory         = reader.ReadClass   (field, obj.memory,         out success);  return success;
            }
            return false;
        }

        private static void Write(ref HostInfo obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString  (fields[Gen_hostVersion],    obj.hostVersion,    ref firstMember);
            writer.WriteString  (fields[Gen_flioxVersion],   obj.flioxVersion,   ref firstMember);
            writer.WriteString  (fields[Gen_hostName],       obj.hostName,       ref firstMember);
            writer.WriteString  (fields[Gen_projectName],    obj.projectName,    ref firstMember);
            writer.WriteString  (fields[Gen_projectWebsite], obj.projectWebsite, ref firstMember);
            writer.WriteString  (fields[Gen_envName],        obj.envName,        ref firstMember);
            writer.WriteString  (fields[Gen_envColor],       obj.envColor,       ref firstMember);
            writer.WriteBoolean (fields[Gen_pubSub],         obj.pubSub,         ref firstMember);
            writer.WriteClass   (fields[Gen_routes],         obj.routes,         ref firstMember);
            writer.WriteClass   (fields[Gen_memory],         obj.memory,         ref firstMember);
        }
    }
}

