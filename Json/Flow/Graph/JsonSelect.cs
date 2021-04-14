﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Friflo.Json.Flow.Graph.Select;

namespace Friflo.Json.Flow.Graph
{
    public class SelectorResult
    {
        public  readonly    List<Scalar>    values = new List<Scalar>();
        private readonly    List<int>       startIndices = new List<int>();
        private             int             lastGroupIndex;

        internal SelectorResult() { }
        
        public void Init() {
            values.Clear();
            startIndices.Clear();
            lastGroupIndex = -1;
        }

        internal void Add(Scalar scalar, PathNode<SelectorResult> parentGroup) {
            if (parentGroup != null) {
                var index = parentGroup.arrayIndex;
                if (index != lastGroupIndex) {
                    lastGroupIndex = index;
                    startIndices.Add(values.Count);
                }
            }
            values.Add(scalar);
        }

        public List<string> AsStringList() {
            var result = new List<string>(values.Count);
            foreach (var item in values) {
                result.Add(item.AsString());
            }
            return result;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            AppendItemAsString(sb);
            return sb.ToString();
        }

        /// Format as debug string - not as JSON 
        private void AppendItemAsString(StringBuilder sb) {
            switch (values.Count) {
                case 0:
                    sb.Append("[]");
                    break;
                case 1:
                    sb.Append('[');
                    values[0].AppendTo(sb);
                    sb.Append(']');
                    break;
                default:
                    sb.Append('[');
                    values[0].AppendTo(sb);
                    for (int n = 1; n < values.Count; n++) {
                        sb.Append(',');
                        values[n].AppendTo(sb);
                    }
                    sb.Append(']');
                    break;
            }
        }
    }
    

    public class JsonSelect : PathNodeTree<SelectorResult>
    {
        internal JsonSelect() { }
        
        public JsonSelect(IList<string> pathList) {
            CreateNodeTree(pathList);
        }

        internal new void CreateNodeTree(IList<string> pathList) {
            base.CreateNodeTree(pathList);
            foreach (var selector in selectors) {
                selector.result = new SelectorResult ();
            }
        }
        
        internal void InitSelectorResults() {
            foreach (var selector in selectors) {
                selector.result.Init();
            }
        }

        public List<SelectorResult> GetResult() {
            var results = new List<SelectorResult>(selectors.Count);
            foreach (var selector in selectors) {
                results.Add(selector.result);
            }
            return results;
        }
    }
}