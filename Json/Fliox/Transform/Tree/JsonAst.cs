﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Json.Burst;

namespace Friflo.Json.Fliox.Transform.Tree
{
    public struct JsonAst
    {
        public      int                 NodesCount  => nodesCount;
        public      JsonAstNode[]       Nodes       => nodes;
        public      JsonAstNodeDebug[]  DebugNodes  => GetDebugNodes();
        
        private     int                 nodesCount;
        private     int                 nodesCapacity;
        private     JsonAstNode[]       nodes;
        private     byte[]              buf;
        private     int                 pos;
        internal    byte[]              Buf => buf;
        
        internal JsonAst(int capacity) {
            nodesCount      = 0;
            nodesCapacity   = capacity;
            nodes           = new JsonAstNode[nodesCapacity];
            buf             = new byte[32];
            pos             = -1;
        }
        
        internal void Init() {
            nodesCount      = 0;
            var constants   = JsonAstReader.NullTrueFalse;
            pos             = constants.Length;
            Buffer.BlockCopy(constants, 0, buf, 0, pos);
        }
        
        public JsonAstNode GetNode(int index) {
            return nodes[index];
        }
        
        public string GetSpanString(in JsonAstSpan span) {
            return Encoding.UTF8.GetString(buf, span.start, span.len);
        }
        
        internal void AddNode(JsonEvent ev, in JsonAstSpan key, in JsonAstSpan value) {
            nodes[nodesCount] = new JsonAstNode(ev, key, value, -1, -1);
            if (++nodesCount < nodesCapacity) {
                return;
            }
            ExtendCapacity();
        }
        
        internal void AddContainerNode(JsonEvent ev, in JsonAstSpan key, int child) {
            nodes[nodesCount] = new JsonAstNode(ev, key, default, child, -1);
            if (++nodesCount < nodesCapacity) {
                return;
            }
            ExtendCapacity();
        }
        
        private void ExtendCapacity() {
            nodesCapacity = 2 * nodesCapacity;
            var newNodes = new JsonAstNode[nodesCapacity];
            for (int n = 0; n < nodesCount; n++) {
                newNodes[n] = nodes[n];
            }
            nodes = newNodes;
        }

        
        internal void SetNodeNext(int index, int next) {
            nodes[index].next = next;
        }
        
        internal JsonAstSpan AddSpan (in Bytes bytes) {
            var len     = bytes.Len;
            int destPos = Reserve(len);
            Buffer.BlockCopy(bytes.buffer.array, bytes.start, buf, destPos, len);
            return new JsonAstSpan(destPos, len);
        }
        
        private int Reserve (int len) {
            int curPos  = pos;
            int newLen  = curPos + len;
            int bufLen  = buf.Length;
            if (curPos + len > bufLen) {
                var doubledLen = 2 * bufLen;
                if (newLen < doubledLen) {
                    newLen = doubledLen;
                }
                var newBuffer = new byte [newLen];
                Buffer.BlockCopy(buf, 0, newBuffer, 0, curPos);
                buf = newBuffer;
            }
            pos += len;
            return curPos;
        }
        
        private JsonAstNodeDebug[] GetDebugNodes() {
            var count       = nodesCount;
            var debugNodes  = new JsonAstNodeDebug[count];
            for (int n = 0; n < count; n++) {
                debugNodes[n] = new JsonAstNodeDebug(nodes[n], buf);
            }
            return debugNodes;
        }
    }
}