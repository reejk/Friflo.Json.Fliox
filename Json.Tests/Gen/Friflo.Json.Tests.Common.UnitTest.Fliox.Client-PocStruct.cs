// Generated by: https://github.com/friflo/Friflo.Json.Fliox#schema
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Tests.Common.UnitTest.Fliox.Client;

// ReSharper disable InconsistentNaming
namespace Gen.Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    static class Gen_PocStruct
    {
        private const int Gen_value = 0;

        private static bool ReadField (ref PocStruct obj, PropField field, ref Reader reader) {
            bool success;
            switch (field.genIndex) {
                case Gen_value: obj.value = reader.ReadInt32 (field, out success);  return success;
            }
            return false;
        }

        private static void Write(ref PocStruct obj, PropField[] fields, ref Writer writer, ref bool firstMember) {
            writer.WriteInt32 (fields[Gen_value], obj.value, ref firstMember);
        }
    }
}

