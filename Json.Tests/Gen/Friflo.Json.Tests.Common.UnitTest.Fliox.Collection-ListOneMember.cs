// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Collection;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.Collection
{
    static class Gen_ListOneMember
    {
        private const int Gen_ints = 0;

        private static bool ReadField (ref ListOneMember obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_ints: obj.ints = reader.ReadClass (field, obj.ints, out success);  return success;
            }
            return false;
        }

        private static void Write(ref ListOneMember obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteClass (fields[Gen_ints], obj.ints, ref firstMember);
        }
    }
}

