using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace crystallize.Parsers
{
	class FeaturesParser : BaseParser
	{
		override public string Name() { return "Features"; }
		override public string XPath() { return ""; } //select nodes by self

		public Dictionary<XmlNode, string> Components = new Dictionary<XmlNode, string>();

		override public void Parse(XmlNode part)
		{
			foreach (XmlNode featureNode in GetNodes(part, "//wixns:Feature")) {
				foreach (XmlNode componentRef in featureNode.ChildNodes) {
					Components.Add(componentRef, featureNode.Attributes["Id"].Value);
				}
			}
			
		}

		public override void Process()
		{
			string featureNodeId = string.Empty;
			foreach (var element in Components) {
				if (element.Value != featureNodeId) {
					XmlNode componentGroupRef = element.Key.ParentNode.OwnerDocument.CreateElement("ComponentGroupRef", nsMgr.LookupNamespace("wixns"));
					XmlAttribute componentGroupRefId = element.Key.ParentNode.OwnerDocument.CreateAttribute("Id");
					componentGroupRefId.Value = element.Key.ParentNode.Attributes["Id"].Value;
					componentGroupRef.Attributes.Append(componentGroupRefId);
					element.Key.ParentNode.AppendChild(componentGroupRef);
					featureNodeId = element.Value;
				}
				element.Key.ParentNode.RemoveChild(element.Key);
			}
			
			
		}
	}
}
