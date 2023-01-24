// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Monitor;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Monitor
{
    static class Gen_UserHits
    {
        private const int Gen_id = 0;
        private const int Gen_clients = 1;
        private const int Gen_counts = 2;

        private static bool ReadField (ref UserHits obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_id:      obj.id      = reader.ReadJsonKey (field, obj.id,      out success);  return success;
                case Gen_clients: obj.clients = reader.ReadClass (field, obj.clients, out success);  return success;
                case Gen_counts:  obj.counts  = reader.ReadClass (field, obj.counts,  out success);  return success;
            }
            return false;
        }

        private static void Write(ref UserHits obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonKey (fields[Gen_id],      obj.id,      ref firstMember);
            writer.WriteClass (fields[Gen_clients], obj.clients, ref firstMember);
            writer.WriteClass (fields[Gen_counts],  obj.counts,  ref firstMember);
        }
    }
}

