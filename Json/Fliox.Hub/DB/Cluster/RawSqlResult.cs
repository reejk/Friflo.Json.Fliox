// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable ReturnTypeCanBeEnumerable.Global
namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    public class RawSqlResult
    {
        // --- public
        /// <summary>number of returned rows</summary>
        [Serialize]     public  int             rowCount    { get; internal set; }
        /// <summary>The column types of a query result</summary>
                        public  FieldType[]     types;
        /// <summary>An array of all query result values. In total: <see cref="rowCount"/> * <see cref="columnCount"/> values</summary>
                        public  JsonArray       values;
                        public  int             columnCount => types.Length;
                        public  RawSqlRow[]     Rows        => rows ?? GetRows();
        
        // --- private / internal
        [Browse(Never)] private int[]           indexArray;
        [Browse(Never)] private RawSqlRow[]     rows;

        public override         string          ToString()  => $"rows: {rowCount}, columns; {columnCount}";

        public RawSqlResult() { }
        public RawSqlResult(FieldType[] types, JsonArray values) {
            this.types      = types;
            this.values     = values;
            this.rowCount   = values.Count / types.Length;
        }

        public   RawSqlRow      GetRow(int row) {
            if (row < 0 || row >= rowCount) throw new IndexOutOfRangeException(nameof(row));
            return new RawSqlRow(this, row);
        }
        
        private RawSqlRow[] GetRows() { 
            var result      = new RawSqlRow[rowCount];
            for (int row = 0; row < rowCount; row++) {
                result[row] = new RawSqlRow(this, row);
            }
            return result;
        }

        internal int[] GetIndexes() {
            if (indexArray != null) {
                return indexArray;
            }
            var indexes = new int[values.Count + 1];
            int n   = 0;
            int pos = 0;
            while (true)
            {
                var type = values.GetItemType(pos, out int next);
                if (type == JsonItemType.End) {
                    break;
                }
                indexes[n++] = pos;
                pos = next;
            }
            indexes[n] = pos;
            return indexArray = indexes;
        }
        
        internal JsonItemType GetValue(int index, int ordinal, out int pos) {
            var valueIndex  = columnCount * index + ordinal;
            var indexes     = GetIndexes();
            pos             = indexes[valueIndex];
            return values.GetItemType(pos);
        }
    }
    
    public readonly struct RawSqlRow
    {
        // --- public
                        public  readonly    int             index;
                        public  readonly    int             count;
        
        // --- private
        [Browse(Never)] private readonly    RawSqlResult    rawResult;
        
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        public  override    string      ToString()          => GetString();

        internal RawSqlRow(RawSqlResult rawResult, int index) {
            this.index      = index;
            this.count      = rawResult.columnCount;
            this.rawResult  = rawResult;
        }
        
        public JsonItemType GetItemType(int ordinal) {
            return rawResult.GetValue(index, ordinal, out _);
        }
        
        public string GetString(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.JSON:
                case JsonItemType.ByteString:   return Utf8.GetString(rawResult.values.ReadByteSpan(pos));
                case JsonItemType.CharString:   return new string(rawResult.values.ReadCharSpan(pos));
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        public int GetInt32(int ordinal) {
            var type = rawResult.GetValue(index, ordinal, out int pos);
            switch (type) {
                case JsonItemType.Uint8:    return rawResult.values.ReadUint8(pos);
                case JsonItemType.Int16:    return rawResult.values.ReadInt16(pos);
                case JsonItemType.Int32:    return rawResult.values.ReadInt32(pos);
            }
            throw new InvalidOperationException($"incompatible column type: {type}");
        }
        
        private string GetString() {
            var indexes = rawResult.GetIndexes();
            var first   = index * count;
            var start   = indexes[first];
            var end     = indexes[first + count];
            var array   = new JsonArray(count, rawResult.values, start, end);
            return array.AsString();
        }
    }
    
    public enum FieldType
    {
        Unknown     =  0,
        //
        Bool        =  1,
        //
        UInt8       =  2,
        Int16       =  3,
        Int32       =  4,
        Int64       =  5,
        //
        String      =  6,
        DateTime    =  7,
        Guid        =  8,
        //
        Float       =  9,
        Double      = 10,
        //
        JSON        = 11,
    }
}