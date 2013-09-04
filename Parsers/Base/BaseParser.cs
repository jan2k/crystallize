using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using crystallize.Utility;

namespace crystallize.Parsers
{
	abstract class BaseParser : IParser
	{
		internal static XmlNode root;
		internal static XmlNamespaceManager nsMgr;
		public static string SourcePath;

		internal StringBuilder result = new StringBuilder();
		List<XmlNode> nodesToRemove = new List<XmlNode>();

		virtual public string Name() { return "Base"; }
		virtual public string XPath() { return String.Empty; }

		virtual public void Parse(XmlNode part) {Logger.Log("Parsing: " + Name());}
		virtual public void Parse(XmlNodeList parts) {Logger.Log("Parsing: " + Name());}
		virtual public void Process() {Logger.Log("... Processing: " + Name());}
		
		static string xmlDeclaration = "<?xml version=\"1.0\"?>";
		static string wixns = " xmlns=\"http://schemas.microsoft.com/wix/2006/wi\"";
		static string utilns = " xmlns:util=\"http://schemas.microsoft.com/wix/UtilExtension\"";
		static string netfxns = " xmlns:netfx=\"http://schemas.microsoft.com/wix/NetFxExtension\"";
		static string iisns = " xmlns:iis=\"http://schemas.microsoft.com/wix/IIsExtension\"";
		internal string fragmentTemplate = xmlDeclaration+"<Wix"+wixns+utilns+netfxns+iisns+"><Fragment>{0}</Fragment></Wix>";
		
		internal static XmlNodeList GetNodes(XmlNode node, string xPath)
		{
			return node.SelectNodes(xPath, nsMgr);
		}
		internal void Finish()
		{
			foreach (var node in nodesToRemove) {
				node.ParentNode.RemoveChild(node);
			}
			File.WriteAllText(Name()+".wxs", String.Format(fragmentTemplate,result.ToString().RemoveWixNS()).SeparateLines());
		}
	}
	class Localizer
	{
		static string xmlDeclaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
		static string wixLocns = " xmlns=\"http://schemas.microsoft.com/wix/2006/localization\"";
		static string locTemplate = xmlDeclaration + "<WixLocalization Culture=\"{1}\" Codepage=\"{2}\"" + wixLocns + ">{0}</WixLocalization>";
		
		public string langName;
		public string langCode;
		
		public Dictionary<string, string> localizations = new Dictionary<string, string>();
		
		public void Process()
		{
			
			StringBuilder result = new StringBuilder();
			foreach (var locString in localizations) {
				result.AppendFormat("<String Id=\"Progress_{0}\" Overridable=\"yes\">{1}</String>", locString.Key, locString.Value);
			}
			File.WriteAllText(langName + ".wxl",
			                  String.Format(locTemplate, result.ToString(), langName, langCode).SeparateLines());
		}
	}
}
