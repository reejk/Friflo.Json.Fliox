// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Host.Event.Compact;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Host.Event.Compact
{
    static class Gen_WriteTaskModel
    {
        private const int Gen_task = 0;
        private const int Gen_cont = 1;
        private const int Gen_set = 2;

        private static bool ReadField (ref WriteTaskModel obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_task: obj.task = reader.ReadJsonValue (field, out success);  return success;
                case Gen_cont: obj.cont = reader.ReadString    (field, obj.cont, out success);  return success;
                case Gen_set:  obj.set  = reader.ReadClass     (field, obj.set,  out success);  return success;
            }
            return false;
        }

        private static void Write(ref WriteTaskModel obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteJsonValue (fields[Gen_task], obj.task, ref firstMember);
            writer.WriteString    (fields[Gen_cont], obj.cont, ref firstMember);
            writer.WriteClass     (fields[Gen_set],  obj.set,  ref firstMember);
        }
    }
}

