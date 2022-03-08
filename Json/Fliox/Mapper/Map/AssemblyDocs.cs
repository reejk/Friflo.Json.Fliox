// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

// ReSharper disable UseNullPropagation
namespace Friflo.Json.Fliox.Mapper.Map
{
    // ------------------------------------ AssemblyDocs ------------------------------------
    internal sealed class AssemblyDocs
    {
        private     readonly    Dictionary <string, AssemblyDoc>   assemblyDocs =  new Dictionary <string, AssemblyDoc >();
    
        private AssemblyDoc GetAssemblyDoc(Assembly assembly) {
            // todo: may use Assembly reference instead of Assembly name as key
            var name = assembly.GetName().Name;
            if (name == null)
                return null;
            if (!assemblyDocs.TryGetValue(name, out var docs)) {
                docs = AssemblyDoc.Load(name, assembly);
                assemblyDocs[name] = docs;
            }
            if (!docs.Available)
                return null;
            return docs;
        }
        
        internal string GetDocs(Assembly assembly, string signature) {
            if (assembly == null)
                return null;
            var docs = GetAssemblyDoc(assembly);
            if (docs == null)
                return null;
            var documentation = docs.GetDocumentation(signature);
            return documentation;
        }
    }

    // ------------------------------------ AssemblyDoc ------------------------------------
    internal sealed class AssemblyDoc
    {
        private   readonly  string                          name;
        private   readonly  Dictionary<string, XElement>    signatures; // is null if no documentation available
        private   readonly  StringBuilder                   sb;
        
        internal            bool                            Available => signatures != null;

        public override     string                          ToString()   => name;

        private AssemblyDoc(string name, Dictionary<string, XElement>  signatures) {
            this.name       = name;
            this.signatures = signatures;
            sb              = new StringBuilder();
        }
        
        internal string GetDocumentation(string signature) {
            if (!signatures.TryGetValue(signature, out var result))
                return null;
            var text    = GetElementText(sb, result);
            text        = text.Trim();
            sb.Clear();
            return text;
        }

        internal static AssemblyDoc Load(string name, Assembly assembly) {
            var assemblyPath    = assembly.Location;
            var assemblyExt     = Path.GetExtension(assembly.Location);
            var docsPath        = assemblyPath.Substring(0, assemblyPath.Length - assemblyExt.Length) + ".xml";
            if (!File.Exists(docsPath))
                return new AssemblyDoc(name, null);

            try {
                var documentation   = XDocument.Load(docsPath);
                var signatures      = GetSignatures (documentation);
                var docs            = new AssemblyDoc(name, signatures);
                return docs;
            } catch  {
                return new AssemblyDoc(name, null);
            }
        }
        
        private static Dictionary<string, XElement> GetSignatures (XDocument documentation) {
            var doc     = documentation.Element("doc");
            if (doc == null)
                return null;
            var members = doc.Element("members");
            if (members == null)
                return null;
            var memberElements  = members.Elements();
            var signatures      = new Dictionary<string, XElement>();

            foreach (XElement element in memberElements) {
                var signature   = element.Attribute("name");
                var summary     = element.Element("summary");
                if (signature == null || summary == null)
                    continue;
                signatures[signature.Value] = summary;
            }
            return signatures;
        }
        
        private static string GetElementText(StringBuilder sb, XElement element) {
            var nodes = element.DescendantNodes();
            // var nodes = element.DescendantsAndSelf();
            // if (element.Value.Contains("Check some new lines")) { int i = 42; }
            foreach (var node in nodes) {
                if (node.Parent != element)
                    continue;
                var nodeText = GetNodeText(node);
                sb.Append(nodeText);
            }
            var text = sb.ToString();
            sb.Clear();
            return text;
        }
        
        private static string GetNodeText (XNode node) {
            if (node is XText text) {
                return TrimLines(text);
            }
            if (node is XElement element) {
                var name    = element.Name.LocalName;
                var value   = element.Value;
                switch (name) {
                    case "see":
                    case "seealso":
                    case "paramref":
                    case "typeparamref":
                        return GetAttributeText(element);
                    case "para":    return GetContainerText(element, "p");
                    case "list":    return GetContainerText(element, "ul");
                    case "item":    return GetContainerText(element, "li");
                    
                    case "br":      return "<br/>";
                    case "b":       return $"<b>{value}</b>";
                    case "i":       return $"<i>{value}</i>";
                    case "c":       return $"<c>{value}</c>";
                    case "code":    return $"<code>{value}</code>";
                    case "returns": return "";
                    default:        return value;
                }
            }
            return "";
        }
        
        private static string GetContainerText (XElement element, string tag) {
            var sb = new StringBuilder();
            var value = GetElementText(sb, element);
            return $"<{tag}>{value}</{tag}>";
        }
        
        private static string GetAttributeText (XElement element) {
            var attributes = element.Attributes();
            // if (element.Value.Contains("TypeValidator")) { int i = 111; }
            foreach (var attribute in attributes) {
                var attributeName = attribute.Name;
                if (attributeName == "cref" || attributeName == "name") {
                    var value       = attribute.Value;
                    var lastIndex   = value.LastIndexOf('.');
                    var typeName    = lastIndex == -1 ? value : value.Substring(lastIndex + 1);
                    return $"<b>{typeName}</b>";                            
                }
                if (attributeName == "href") {
                    var link = attribute.Value;
                    return $"<a href='{link}'>{link}</a>";
                }
            }
            return "";
        }
        
        /// <summary>Trim leading tabs and spaces. Normalize new lines</summary>
        private static string TrimLines (XText text) {
            string value    = text.Value;
            value           = value.Replace("\r\n", "\n");
            var lines       = value.Split("\n");
            if (lines.Length == 1)
                return value;
            var sb      = new StringBuilder();
            bool first  = true;
            foreach (var line in lines) {
                if (first) {
                    first = false;
                    sb.Append(line);
                    continue;
                }
                sb.Append('\n');
                sb.Append(line.TrimStart());
            }
            return sb.ToString();
        }
    }
}