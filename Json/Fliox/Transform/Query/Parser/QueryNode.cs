// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    // todo - check: change to struct
    internal class QueryNode {
        internal            Token           operation;
        internal readonly   List<QueryNode> operands;
        
        internal            int             Count           => operands.Count;        
        internal            QueryNode       this[int index] => operands[index];
        
        public   override   string          ToString() {
            var sb = new StringBuilder();
            AppendLabel(sb);
            return sb.ToString();
        }
        
        private void AppendLabel (StringBuilder sb) {
            sb.Append(operation.ToString());
            var shape = Token.Shape(operation.type);
            if (shape.arity == Arity.Unary)
                return;
            sb.Append("(");
            if (operands.Count > 0) {
                operands[0].AppendLabel(sb);
            }
            for (int n = 1; n < operands.Count; n++) {
                sb.Append(", ");
                operands[n].AppendLabel(sb);
            }
            sb.Append(")");
        }

        internal QueryNode (Token operation) {
            this.operation = operation;
            operands = new List<QueryNode>();
        }
    } 
}