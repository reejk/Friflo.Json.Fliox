// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_ClientResult
    {
        private const int Gen_queueEvents = 0;
        private const int Gen_queuedEvents = 1;
        private const int Gen_clientId = 2;
        private const int Gen_subscriptionEvents = 3;

        private static bool ReadField (ref ClientResult obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_queueEvents:        obj.queueEvents        = reader.ReadBoolean (field, out success);  return success;
                case Gen_queuedEvents:       obj.queuedEvents       = reader.ReadInt32   (field, out success);  return success;
                case Gen_clientId:           obj.clientId           = reader.ReadJsonKey (field, out success);  return success;
                case Gen_subscriptionEvents: obj.subscriptionEvents = reader.ReadStructNull (field, obj.subscriptionEvents, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ClientResult obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteBoolean (fields[Gen_queueEvents],        obj.queueEvents,        ref firstMember);
            writer.WriteInt32   (fields[Gen_queuedEvents],       obj.queuedEvents,       ref firstMember);
            writer.WriteJsonKey (fields[Gen_clientId],           obj.clientId,           ref firstMember);
            writer.WriteStructNull (fields[Gen_subscriptionEvents], obj.subscriptionEvents, ref firstMember);
        }
    }
}

