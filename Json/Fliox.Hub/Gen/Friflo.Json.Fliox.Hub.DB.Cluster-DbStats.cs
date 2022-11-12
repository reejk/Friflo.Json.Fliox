// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_DbStats
    {
        private const int Gen_containers = 0;

        private static bool ReadField (ref DbStats obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_containers: obj.containers = reader.ReadClass (field, obj.containers, out success);  return success;
            }
            return false;
        }

        private static void Write(ref DbStats obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteClass (fields[Gen_containers], obj.containers, ref firstMember);
        }
    }
}

