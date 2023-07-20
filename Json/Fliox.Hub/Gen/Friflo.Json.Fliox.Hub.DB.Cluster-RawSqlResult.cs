// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Hub.DB.Cluster;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Fliox.Hub.DB.Cluster
{
    static class Gen_RawSqlResult
    {
        private const int Gen_types = 0;
        private const int Gen_values = 1;

        private static bool ReadField (ref RawSqlResult obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_types:  obj.types  = reader.ReadClass (field, obj.types,  out success);  return success;
                case Gen_values: obj.values = reader.ReadClass (field, obj.values, out success);  return success;
            }
            return false;
        }

        private static void Write(ref RawSqlResult obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteClass (fields[Gen_types],  obj.types,  ref firstMember);
            writer.WriteClass (fields[Gen_values], obj.values, ref firstMember);
        }
    }
}

