// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Friflo.Json.Fliox.Schema.Definition;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

namespace Friflo.Json.Fliox.Hub.PostgreSQL
{
    public static class PostgreSQLExtensions
    {
        public static string PostgresFilter(this FilterOperation op, TypeDef entityType) {
            var cx      = new ConvertContext("c", op, entityType);
            var result  = cx.Traverse(op);
            return result;
        }
    }
    
    internal sealed class ConvertContext {
        private readonly   string           collection;
        private readonly   string           collectionStart;
        private readonly   FilterOperation  filterOp;
        private readonly   TypeDef          entityType;
        
        internal ConvertContext (string collection, FilterOperation filterOp, TypeDef entityType) {
            this.collection = collection;
            this.entityType = entityType;
            if (filterOp is Filter filter) {
                collectionStart = $"{filter.arg}.";
            }
            this.filterOp     = filterOp;
        }
        
        /// <summary>
        /// Create CosmosDB query filter specified at: 
        /// https://github.com/friflo/Friflo.Json.Fliox/tree/main/Json/Fliox.Hub/Client#query-filter
        /// </summary>
        internal string Traverse(Operation operation) {
            switch (operation) {
                case Field field: {
                    if (collectionStart != null && field.name.StartsWith(collectionStart)) {
                        var fieldName = field.name.Substring(collectionStart.Length);
                        return $"(data ->> '{fieldName}')";
                    }
                    throw new InvalidOperationException($"expect field {field.name} starts with {collectionStart}");
                }
                
                // --- literal --- 
                case StringLiteral stringLiteral:
                    return $"'{stringLiteral.value}'";
                case DoubleLiteral doubleLiteral:
                    var dbl = doubleLiteral.value.ToString(CultureInfo.InvariantCulture);
                    return $"{dbl}";
                case LongLiteral longLiteral:
                    var lng = longLiteral.value.ToString(); 
                    return $"{lng}";
                case TrueLiteral    _:
                    return "true";
                case FalseLiteral   _:
                    return "false";
                case NullLiteral    _:
                    return "null";
                
                // --- compare ---
                case Equal equal: {
                    var left    = Traverse(equal.left);
                    var right   = Traverse(equal.right);
                    GetCasts(equal, out var leftCast, out var rightCast);
                    if (left  == "null") return $"({right} IS null)";
                    if (right == "null") return $"({left} IS null)";
                    return $"{left}{leftCast} = {right}{rightCast}";
                }
                case NotEqual notEqual: {
                    var left    = Traverse(notEqual.left);
                    var right   = Traverse(notEqual.right);
                    if (left  == "null") return $"({right} IS NOT null)";
                    if (right == "null") return $"({left} IS NOT null)";
                    GetCasts(notEqual, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} != {right}{rightCast}";
                }
                case Less lessThan: {
                    var left    = Traverse(lessThan.left);
                    var right   = Traverse(lessThan.right);
                    GetCasts(lessThan, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} < {right}{rightCast}";
                }
                case LessOrEqual lessThanOrEqual: {
                    var left    = Traverse(lessThanOrEqual.left);
                    var right   = Traverse(lessThanOrEqual.right);
                    GetCasts(lessThanOrEqual, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} <= {right}{rightCast}";
                }
                case Greater greaterThan: {
                    var left    = Traverse(greaterThan.left);
                    var right   = Traverse(greaterThan.right);
                    GetCasts(greaterThan, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} > {right}{rightCast}";
                }
                case GreaterOrEqual greaterThanOrEqual: {
                    var left    = Traverse(greaterThanOrEqual.left);
                    var right   = Traverse(greaterThanOrEqual.right);
                    GetCasts(greaterThanOrEqual, out var leftCast, out var rightCast);
                    return $"{left}{leftCast} >= {right}{rightCast}";
                }
                
                // --- logical ---
                case Not @not:
                    var operand = Traverse(@not.operand);
                    return $"NOT({operand})";
                case Or or:
                    var operands = GetOperands(or.operands);
                    return string.Join(" OR ", operands);
                case And and:
                    operands = GetOperands(and.operands);
                    return string.Join(" AND ", operands);
                
                // --- string ---
                case StartsWith startsWith: {
                    var left    = Traverse(startsWith.left);
                    var right   = Traverse(startsWith.right);
                    return $"{left} LIKE '{UnString(right)}%'";
                }
                case EndsWith endsWith: {
                    var left    = Traverse(endsWith.left);
                    var right   = Traverse(endsWith.right);
                    return $"{left} LIKE '%{UnString(right)}'";
                }
                case Contains contains: {
                    var left    = Traverse(contains.left);
                    var right   = Traverse(contains.right);
                    return $"{left} LIKE '%{UnString(right)}%'";
                }
                case Length length: {
                    var value = Traverse(length.value);
                    return $"LENGTH({value})";
                }
                
                // --- arithmetic: operators ---
                case Add add: {
                    var left    = Traverse(add.left);
                    var right   = Traverse(add.right);
                    return $"{left} + {right}";
                }
                case Subtract subtract: {
                    var left    = Traverse(subtract.left);
                    var right   = Traverse(subtract.right);
                    return $"{left} - {right}";
                }
                case Multiply multiply: {
                    var left    = Traverse(multiply.left);
                    var right   = Traverse(multiply.right);
                    return $"{left} * {right}";
                }
                case Divide divide: {
                    var left    = Traverse(divide.left);
                    var right   = Traverse(divide.right);
                    return $"{left} / {right}";
                }
                case Modulo modulo: {
                    var left    = Traverse(modulo.left);
                    var right   = Traverse(modulo.right);
                    return $"{left} % {right}";
                }
                
                // --- arithmetic: methods ---
                case Abs abs: {
                    var value = Traverse(abs.value);
                    return $"ABS({value})";
                }
                case Ceiling ceiling: {
                    var value = Traverse(ceiling.value);
                    return $"ROUND({value}+0.5)";
                }
                case Floor floor: {
                    var value = Traverse(floor.value);
                    return $"ROUND({value}-0.5)";
                }
                case Exp exp: {
                    var value = Traverse(exp.value);
                    return $"EXP({value})";
                }
                case Log log: {
                    var value = Traverse(log.value);
                    return $"LN({value})";
                }
                case Sqrt sqrt: {
                    var value = Traverse(sqrt.value);
                    return $"SQRT({value})";
                }
                
                // --- constants ---
                case PiLiteral:
                    return "PI()";
                case EulerLiteral:
                    return "EXP(1)";
                
                // --- aggregate ---
                case CountWhere countWhere: {
                    var cx              = new ConvertContext ("", filterOp, null);
                    operand             = cx.Traverse(countWhere.predicate);
                    string fieldName    = Traverse(countWhere.field);
                    string arg          = countWhere.arg;
                    return $"(SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand})";
                }
                // --- quantify ---
                case Any any: {
                    var cx              = new ConvertContext ("", filterOp, null);
                    operand             = cx.Traverse(any.predicate);
                    string fieldName    = Traverse(any.field);
                    var arg             = any.arg;
                    return $"EXISTS(SELECT VALUE {arg} FROM {arg} IN {fieldName} WHERE {operand})";
                }
                case All all: {
                    var cx              = new ConvertContext ("", filterOp, null);
                    operand             = cx.Traverse(all.predicate);
                    var fieldName       = Traverse(all.field);
                    var arg             = all.arg;
                    // treat array == null and missing array as empty array <=> array[]
                    return $"IS_NULL({fieldName}) OR NOT IS_DEFINED({fieldName}) OR (SELECT VALUE Count(1) FROM {arg} IN {fieldName} WHERE {operand}) = ARRAY_LENGTH({fieldName})";
                }
                // --- query filter expression
                case Filter filter: {
                    var cx              = new ConvertContext (collection, filterOp, entityType);
                    operand             = cx.Traverse(filter.body);
                    return $"{operand}";
                }
                default:
                    throw new NotImplementedException($"missing conversion for operation: {operation}, filter: {filterOp}");
            }
        }
        
        private void GetCasts(BinaryBoolOp op, out string leftCast, out string rightCast) {
            leftCast    = GetCast(op.left);
            rightCast   = GetCast(op.right);
        }

        private string GetCast(Operation op) {
            if (op is Field field) {
                var path = field.name.Substring(field.arg.Length + 1);
                var fieldType = entityType.Fields.First(f => f.name == path);
                switch (fieldType.type.TypeId) {
                    case StandardTypeId.Uint8:
                    case StandardTypeId.Int16:
                    case StandardTypeId.Int32:
                    case StandardTypeId.Int64:
                    case StandardTypeId.Float:
                    case StandardTypeId.Double:
                        return "::numeric";
                }
            }
            return "";
        }
        
        private string[] GetOperands (List<FilterOperation> operands) {
            var result = new string[operands.Count];
            for (int n = 0; n < operands.Count; n++) {
                result[n] = Traverse(operands[n]);
            }
            return result;
        }
        
        private static string UnString(string value) {
            if (value[0] == '\'') {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}