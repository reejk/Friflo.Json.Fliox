// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Fliox.Schema.Definition;

namespace Friflo.Json.Fliox.Hub.Host.SQL
{
    public sealed class ColumnInfo
    {
        public readonly     int             ordinal;
        public readonly     string          name;
        public readonly     string          memberName;
        public readonly     StandardTypeId  typeId;

        public override     string          ToString() => $"{name} [{ordinal}] : {typeId}";

        public ColumnInfo (int ordinal, string name, string memberName, StandardTypeId typeId) {
            this.ordinal    = ordinal;
            this.name       = name;
            this.memberName = memberName;
            this.typeId     = typeId;
        }
    }
    
    public readonly struct ObjectInfo
    {
        public readonly     string          name;
        public readonly     ColumnInfo[]    columns;
        public readonly     ObjectInfo[]    objects;

        public override     string          ToString() => name ?? "(Root)";

        public ObjectInfo (string name, ColumnInfo[] columns, ObjectInfo[] objects) {
            this.name       = name;
            this.columns    = columns;
            this.objects    = objects;
        }
    }
    
    public sealed class TableInfo
    {
        public   readonly   ColumnInfo[]                    columns;
        public   readonly   ColumnInfo                      keyColumn;
        // --- internal
        private  readonly   string                          container;
        private  readonly   Dictionary<string, ColumnInfo>  columnMap;
        private  readonly   Dictionary<string, ColumnInfo>  indexMap;
        private  readonly   ObjectInfo                      root;

        public   override   string                          ToString() => container;

        public TableInfo(EntityDatabase database, string container) {
            this.container  = container;
            columnMap       = new Dictionary<string, ColumnInfo>();
            indexMap        = new Dictionary<string, ColumnInfo>();
            var type        = database.Schema.typeSchema.RootType.FindField(container).type;
            root            = AddTypeFields(type, null, null);
            keyColumn       = columnMap[type.KeyField.name];
            columns         = new ColumnInfo[columnMap.Count];
            foreach (var pair in columnMap) {
                var column = pair.Value;
                columns[column.ordinal] = column;
            }
        }
        
        private ObjectInfo AddTypeFields(TypeDef type, string prefix, string name) {
            var fields      = type.Fields;
            var columnList  = new List<ColumnInfo>();
            var objectList  = new List<ObjectInfo>();
            foreach (var field in fields) {
                var fieldPath   = prefix == null ? field.name : prefix + "." + field.name;
                var fieldType   = field.type;
                if (fieldType.IsClass) {
                    var obj = AddTypeFields(fieldType, fieldPath, field.name);
                    objectList.Add(obj);
                    continue;
                }
                var typeId      = fieldType.TypeId;
                if (typeId == StandardTypeId.None) {
                    continue;
                }
                var isScalar    = !field.isArray && !field.isDictionary;
                if (isScalar) {
                    var column = new ColumnInfo(columnMap.Count, fieldPath, field.name, typeId);
                    columnList.Add(column);
                    columnMap.Add(fieldPath, column);
                    indexMap.Add(fieldPath, column);
                }
            }
            return new ObjectInfo(name, columnList.ToArray(), objectList.ToArray());
        }
    }
}