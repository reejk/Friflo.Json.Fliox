// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol
{
    static class Gen_EventMessage
    {
        private const int Gen_dstClientId = 0;
        private const int Gen_events = 1;

        private static bool ReadField (ref EventMessage obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_dstClientId: obj.dstClientId = reader.ReadJsonKey (field, out success);  return success;
                case Gen_events:      obj.events      = reader.ReadClass   (field, obj.events,      out success);  return success;
            }
            return false;
        }

        private static void Write(ref EventMessage obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonKey (fields[Gen_dstClientId], obj.dstClientId, ref firstMember);
            writer.WriteClass   (fields[Gen_events],      obj.events,      ref firstMember);
        }
    }
}

