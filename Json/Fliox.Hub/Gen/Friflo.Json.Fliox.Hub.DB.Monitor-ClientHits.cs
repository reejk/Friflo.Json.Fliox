// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Monitor;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Monitor
{
    static class Gen_ClientHits
    {
        private const int Gen_id = 0;
        private const int Gen_user = 1;
        private const int Gen_counts = 2;
        private const int Gen_subscriptionEvents = 3;

        private static bool ReadField (ref ClientHits obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_id:                 obj.id                 = reader.ReadJsonKey (field, out success);  return success;
                case Gen_user:               obj.user               = reader.ReadJsonKey (field, out success);  return success;
                case Gen_counts:             obj.counts             = reader.ReadClass   (field, obj.counts,             out success);  return success;
                case Gen_subscriptionEvents: obj.subscriptionEvents = reader.ReadStructNull (field, obj.subscriptionEvents, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ClientHits obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonKey (fields[Gen_id],                 obj.id,                 ref firstMember);
            writer.WriteJsonKey (fields[Gen_user],               obj.user,               ref firstMember);
            writer.WriteClass   (fields[Gen_counts],             obj.counts,             ref firstMember);
            writer.WriteStructNull (fields[Gen_subscriptionEvents], obj.subscriptionEvents, ref firstMember);
        }
    }
}

