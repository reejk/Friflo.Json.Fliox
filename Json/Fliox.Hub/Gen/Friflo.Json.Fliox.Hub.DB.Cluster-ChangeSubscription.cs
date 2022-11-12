// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_ChangeSubscription
    {
        private const int Gen_container = 0;
        private const int Gen_changes = 1;
        private const int Gen_filter = 2;

        private static bool ReadField (ref ChangeSubscription obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_container: obj.container = reader.ReadString (field, out success);  return success;
                case Gen_changes:   obj.changes   = reader.ReadClass  (field, obj.changes,   out success);  return success;
                case Gen_filter:    obj.filter    = reader.ReadString (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ChangeSubscription obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString (fields[Gen_container], obj.container, ref firstMember);
            writer.WriteClass  (fields[Gen_changes],   obj.changes,   ref firstMember);
            writer.WriteString (fields[Gen_filter],    obj.filter,    ref firstMember);
        }
    }
}

