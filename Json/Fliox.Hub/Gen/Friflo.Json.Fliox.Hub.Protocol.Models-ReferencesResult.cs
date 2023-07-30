// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.Protocol.Models;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.Protocol.Models
{
    static class Gen_ReferencesResult
    {
        private const int Gen_error = 0;
        private const int Gen_container = 1;
        private const int Gen_len = 2;
        private const int Gen_set = 3;
        private const int Gen_references = 4;

        private static bool ReadField (ref ReferencesResult obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_error:      obj.error      = reader.ReadString    (field, obj.error,      out success);  return success;
                case Gen_container:  obj.container  = reader.ReadShortString (field, obj.container,  out success);  return success;
                case Gen_len:        obj.len        = reader.ReadInt32Null (field, out success);  return success;
                case Gen_set:        obj.set        = reader.ReadClass     (field, obj.set,        out success);  return success;
                case Gen_references: obj.references = reader.ReadClass     (field, obj.references, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ReferencesResult obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteString    (fields[Gen_error],      obj.error,      ref firstMember);
            writer.WriteShortString (fields[Gen_container],  obj.container,  ref firstMember);
            writer.WriteInt32Null (fields[Gen_len],        obj.len,        ref firstMember);
            writer.WriteClass     (fields[Gen_set],        obj.set,        ref firstMember);
            writer.WriteClass     (fields[Gen_references], obj.references, ref firstMember);
        }
    }
}

