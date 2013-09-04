using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using crystallize.Utility;

namespace crystallize.Parsers
{
	class MainParser : BaseParser
	{
		List<IParser> parsers = new List<IParser>();
		Dictionary<string, IParser> sections =  new Dictionary<string, IParser>();

		XmlNodeList binaries;
		XmlNodeList customActions;
		XmlNodeList properties;
		XmlNodeList icons;

		public MainParser()
		{
			parsers.Add(new FilesParser());
			parsers.Add(new FeaturesParser());
			parsers.Add(new UIParser());
			//parsers.Add(new SequencesParser());
		}
		
		override public string Name() { return "Main"; }

		override public void Parse(XmlNode part)
		{
			if (part.Name != "Wix") return;
			root = part;
			XmlNode subpart;
			nsMgr = new XmlNamespaceManager(part.OwnerDocument.NameTable);
			nsMgr.AddNamespace("wixns", part.NamespaceURI);
			
			binaries = part.SelectNodes("//wixns:Binary",nsMgr);
			customActions = part.SelectNodes("//wixns:CustomAction", nsMgr);
			properties = part.SelectNodes("//wixns:Property", nsMgr);
			icons = part.SelectNodes("//wixns:Icon", nsMgr);
			
			foreach (var parser in parsers) {
				subpart = parser.XPath() != String.Empty ? part.SelectSingleNode(parser.XPath(), nsMgr) : part;
				if (subpart != null)
				{
					parser.Parse(subpart);
					sections.Add(parser.Name(), parser);
					Logger.Log("Found: " + parser.Name() + " section");
				}
			}
			
			((FilesParser)sections["Files"]).Components = ((FeaturesParser)sections["Features"]).Components;
			
			if (sections.ContainsKey("UI")) {
				var uiProps = ((UIParser)sections["UI"]).UiProperties;
				var uiBins = ((UIParser)sections["UI"]).UiBinaries;
				foreach (XmlNode element in properties) {
					if (uiProps.ContainsKey(element.Attributes["Id"].Value)) {
						uiProps[element.Attributes["Id"].Value] = element;
						foreach (XmlNode element2 in binaries) {
							if (element.Attributes["Value"].Value == element2.Attributes["Id"].Value) {
								uiBins.Add(element2.Attributes["Id"].Value, element2);
							}
						}
					}
				}
				foreach (XmlNode element in binaries) {
					if (uiBins.ContainsKey(element.Attributes["Id"].Value)) {
						uiBins[element.Attributes["Id"].Value] = element;
					}
				}
			}
		}

		override public void Process()
		{
			foreach (var sectionName in sections.Keys) {
				Logger.Log("Processing: " + sectionName);
				sections[sectionName].Process();
			}
			StringBuilder ca = new StringBuilder();
			HashSet<string> ca_bins = new HashSet<string>();
			foreach (XmlNode caNode in customActions) {
				ca.Append(caNode.OuterXml);
				if (caNode.Attributes["BinaryKey"] != null) ca_bins.Add(caNode.Attributes["BinaryKey"].Value);
				caNode.ParentNode.RemoveChild(caNode);
			}
			foreach (XmlNode binNode in binaries) {
				if (ca_bins.Contains(binNode.Attributes["Id"].Value)) {
					if (!Directory.Exists("CA")) Directory.CreateDirectory("CA");
					File.Copy(SourcePath + binNode.Attributes["SourceFile"].Value, binNode.Attributes["SourceFile"].Value.Replace(".\\Binary\\",".\\CA\\"),true);
					ca.Append(binNode.OuterXml.Replace(".\\Binary\\",".\\CA\\"));
					binNode.ParentNode.RemoveChild(binNode);
				}
				else
				{
					if (!Directory.Exists("Binary")) Directory.CreateDirectory("Binary");
					File.Copy(SourcePath + binNode.Attributes["SourceFile"].Value, binNode.Attributes["SourceFile"].Value, true);
				}
			}
			foreach (XmlNode iconNode in icons) {
				if (!Directory.Exists("Icon")) Directory.CreateDirectory("Icon");
				File.Copy(SourcePath + iconNode.Attributes["SourceFile"].Value, iconNode.Attributes["SourceFile"].Value, true);
			}
			File.WriteAllText("CustomActions.wxs", String.Format(fragmentTemplate, ca.ToString().RemoveWixNS()).SeparateLines());
		}
	}
}
