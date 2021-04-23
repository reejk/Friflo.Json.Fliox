﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Friflo.Json.EntityGraph
{
    public struct SetInfo
    {
        public  int     peers;
        public  int     tasks;
        //
        public  int     create;
        public  int     read;
        public  int     readRef;
        public  int     queries;
        public  int     patch;
        public  int     delete;

        private static void  Append(StringBuilder sb, string label, int count, ref bool first) {
            if (count == 0)
                return;
            if (!first) {
                sb.Append(", ");
            }
            sb.Append(label);
            sb.Append(" #");
            sb.Append(count);
            first = false;
        }
        
        internal static void  AppendTasks(StringBuilder sb, string label, int count, ref bool first) {
            if (count == 0)
                return;
            if (!first) {
                sb.Append(", ");
            }
            sb.Append(label);
            sb.Append(": ");
            sb.Append(count);
            first = false;
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("peers");
            sb.Append(": ");
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                AppendTasks(sb, "tasks",    tasks,      ref first);
                first = true;
                sb.Append(" -> ");
                Append(sb,  "create",       create,     ref first);
                Append(sb,  "read",         read,       ref first);
                if (readRef > 0) {
                    sb.Append("(");
                    Append(sb, "ref",       readRef,    ref first);
                    sb.Append(")");
                }
                AppendTasks(sb, "queries",  queries,    ref first);
                Append(sb,  "patch",        patch,      ref first);
                Append(sb,  "delete",       delete,     ref first);
            }
            return sb.ToString();
        }
    }

    public struct StoreInfo
    {
        public  int     peers;
        public  int     tasks;

        internal StoreInfo(Dictionary<Type, EntitySet> setByType) {
            peers = 0;
            tasks = 0;
            foreach (var pair in setByType)
                Add(pair.Value.SetInfo);
        }
        
        private void Add(in SetInfo info) {
            peers += info.peers;
            tasks += info.tasks;
        }
        
        public override string ToString() {
            var sb = new StringBuilder();
            sb.Append("peers");
            sb.Append(": ");
            sb.Append(peers);
            
            if (tasks > 0) {
                bool first = false;
                SetInfo.AppendTasks(sb, "tasks", tasks, ref first);
            }
            return sb.ToString();
        }
    } 
}
