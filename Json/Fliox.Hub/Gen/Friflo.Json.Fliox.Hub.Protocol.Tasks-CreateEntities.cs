// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    static class Gen_CreateEntities
    {
        private const int Gen_info = 0;
        private const int Gen_container = 1;
        private const int Gen_reservedToken = 2;
        private const int Gen_keyName = 3;
        private const int Gen_entities = 4;

        private static bool ReadField (ref CreateEntities obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_info:          obj.info          = reader.ReadJsonValue (field, out success);  return success;
                case Gen_container:     obj.container     = reader.ReadString    (field, out success);  return success;
                case Gen_reservedToken: obj.reservedToken = reader.ReadStructNull (field, obj.reservedToken, out success);  return success;
                case Gen_keyName:       obj.keyName       = reader.ReadString    (field, out success);  return success;
                case Gen_entities:      obj.entities      = reader.ReadClass     (field, obj.entities,      out success);  return success;
            }
            return false;
        }

        private static void Write(ref CreateEntities obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonValue (fields[Gen_info],          obj.info,          ref firstMember);
            writer.WriteString    (fields[Gen_container],     obj.container,     ref firstMember);
            writer.WriteStructNull (fields[Gen_reservedToken], obj.reservedToken, ref firstMember);
            writer.WriteString    (fields[Gen_keyName],       obj.keyName,       ref firstMember);
            writer.WriteClass     (fields[Gen_entities],      obj.entities,      ref firstMember);
        }
    }
}

