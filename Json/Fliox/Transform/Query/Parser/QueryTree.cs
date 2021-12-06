// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Friflo.Json.Fliox.Transform.Query.Parser
{
    // example:  a * b + c   (5 tokens)
    //
    //  a (1)       * (2)       b (3)       + (4)       c (5)
    //             
    //                a           a             a           a
    //               /           /             /           /
    //  a           *           *             *           *
    //                           \           / \         / \  
    //                            b         +   b       +   b       
    //                                                   \
    //                                                    c
    // For understanding:
    // operator with lowest precedence (+) is root.
    // Sounds irritating on first look but make sense in design of expression trees.
    //
    public static class QueryTree
    {
        public static QueryNode CreateTree (string operation, out string error) {
            var result  = QueryLexer.Tokenize (operation,   out error);
            var node    = CreateTree(result.items,out error);
            return node;
        }

        internal static QueryNode CreateTree(Token[] tokens, out string error) {
            error       = null;
            int pos     = 0;
            var stack   = new Stack<QueryNode>();
            while (pos < tokens.Length) {
                var token = tokens[pos++];
                GetNode(stack, token, out error);
            }
            if (stack.Count == 0)
                throw new InvalidOperationException("invalid stack count");
            while (stack.Count > 1)
                stack.Pop();
            var result = stack.Peek();
            return result;
        }

        private static void GetNode(Stack<QueryNode> stack, in Token token, out string error) {
            var shape = Token.Shape(token.type);
            switch (shape.arity) {
                case Arity.Unary:
                    AddUnary (stack, token, out error);
                    return;
                case Arity.Binary:
                    AddBinary(stack, token, out error);
                    return;
                case Arity.NAry:
                    AddNAry  (stack, token, out error);
                    return;
                default:
                    error = $"Invalid arity for token: {token}";
                    return;
            }
        }
        
        private static void AddUnary(Stack<QueryNode> stack, in Token token, out string error) {
            error = null;
            var newNode = new QueryNode(token);
            if (stack.Count == 0) {
                stack.Push(newNode);
                return;                
            }
            var node = stack.Peek();
            node.operands.Add(newNode);
        }

        private static void AddBinary(Stack<QueryNode> stack, in Token token, out string error) {
            error = null;
            var newNode = new QueryNode(token);
            
            var node    = stack.Peek();
            var last    = node.operands.Count - 1;
            var precedence  = newNode.precedence;
            if (newNode.arity != Arity.Unary && node.arity != Arity.Unary) {
                while (true) {
                    if (precedence < node.precedence) {
                        // replace last operand of stack.Head
                        newNode.operands.Add(node.operands[last]);
                        node.operands[last] = newNode;
                        stack.Push(newNode);
                        return;
                    }
                    stack.Pop();
                    if (stack.Count == 0)
                        break;
                    node = stack.Peek();
                }
            } else {
                stack.Pop();    
            }            
            newNode.operands.Add(node);
            stack.Push(newNode);
        }

        private static void AddNAry(Stack<QueryNode> stack, in Token token, out string error) {
            var node = stack.Peek();
            if (node.operation.type == token.type) {
                // || &&  are allowed having multiple operators 
                error = null;
                return;
            }
            stack.Pop();
            error = null;
            var newNode = new QueryNode(token);
            newNode.operands.Add(node);
            stack.Push(newNode);
        }
    }
}