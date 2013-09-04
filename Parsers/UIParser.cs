using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using crystallize.Utility;

namespace crystallize.Parsers
{
	class UIParser : BaseParser
	{
		override public string Name() { return "UI"; }
		override public string XPath() { return "//wixns:UI"; }

		XmlNode uiPart;
		Dictionary<int, string> errors = new Dictionary<int, string>();
		Dictionary<string, string> uiText = new Dictionary<string, string>();
		Dictionary<string, string> progressText = new Dictionary<string, string>();
		List<string> dialogs = new List<string>();

		public Dictionary<string, XmlNode> UiBinaries = new Dictionary<string, XmlNode>();
		public Dictionary<string, XmlNode> UiProperties = new Dictionary<string, XmlNode>();

		Dictionary<string, XmlAttribute> localizableAttrs = new Dictionary<string, XmlAttribute>();
		override public void Parse(XmlNode part)
		{
			uiPart = part;

			foreach (XmlNode element in uiPart.ChildNodes) {
				if (element.Name == "Error") {
					errors.Add(int.Parse(element.Attributes["Id"].Value), element.InnerXml);
				}
				if (element.Name == "UIText") {
					uiText.Add(element.Attributes["Id"].Value, element.InnerXml);
				}
				if (element.Name == "ProgressText") {
					progressText.Add(element.Attributes["Action"].Value, element.InnerXml);
					if (element.Attributes["Template"] != null) progressText.Add(element.Attributes["Action"].Value + "_Template", element.Attributes["Template"].Value);
				}
				if (element.Name == "Dialog") {
					dialogs.Add(element.Attributes["Id"].Value);
					localizableAttrs.Add(element.Attributes["Id"].Value + "Title", element.Attributes["Title"]);
					foreach (XmlNode elementWithText in GetNodes(element, "wixns:Control[@Text]")) {
						XmlAttribute textAttr = elementWithText.Attributes["Text"];
						string val = textAttr.Value;
						if (val.StartsWith("[") && val.EndsWith("]") && !val.Substring(1,val.Length-2).Contains("]"))
						{
							if (!UiProperties.ContainsKey(val.Substring(1,val.Length-2)))
								UiProperties.Add(val.Substring(1,val.Length-2), null);
						}
						else
						{
							if ((elementWithText.Attributes["Type"].Value != "Bitmap") && (elementWithText.Attributes["Type"].Value != "Icon"))
								localizableAttrs.Add(element.Attributes["Id"].Value + elementWithText.Attributes["Id"].Value + "Text", textAttr);
							else
								if (!UiBinaries.ContainsKey(val))
									UiBinaries.Add(val, null);
						}
					}
				}
			}
		}

		public override void Process()
		{
			StringBuilder localizations = new StringBuilder();
			
			StringBuilder sb = new StringBuilder();
			foreach (var element in errors) {
				sb.AppendFormat("<Error Id=\"{0}\">!(loc.Error{0})</Error>",element.Key);
				localizations.AppendFormat("<String Id=\"Error{0}\" Overridable=\"yes\">{1}</String>",element.Key,element.Value);
			}
			File.WriteAllText("ErrorText.wxs", String.Format(fragmentTemplate, "<UI>"+sb.ToString()+"</UI>").SeparateLines());
			
			sb = new StringBuilder();
			foreach (var element in progressText) {
				if (!element.Key.EndsWith("_Template")) {
					if (progressText.ContainsKey(element.Key + "_Template"))
						sb.AppendFormat("<ProgressText Action=\"{0}\" Template=\"{1}\">!(loc.Progress_{0})</ProgressText>", element.Key, progressText[element.Key + "_Template"]);
					else
						sb.AppendFormat("<ProgressText Action=\"{0}\">!(loc.Progress_{0})</ProgressText>", element.Key);
					localizations.AppendFormat("<String Id=\"Progress_{0}\" Overridable=\"yes\">{1}</String>",element.Key,element.Value);
				}
				
			}
			File.WriteAllText("ProgressText.wxs", String.Format(fragmentTemplate, "<UI Id=\"ProgressText\">"+sb.ToString()+"</UI>").SeparateLines());
			
			sb = new StringBuilder();
			foreach (var element in uiText) {
				sb.AppendFormat("<UIText Id=\"{0}\">!(loc.UI_{0})</UIText>",element.Key);
				localizations.AppendFormat("<String Id=\"UI_{0}\" Overridable=\"yes\">{1}</String>",element.Key,element.Value);
			}
			File.WriteAllText("UIText.wxs", String.Format(fragmentTemplate, "<UI Id=\"UIText\">"+sb.ToString()+"</UI>").SeparateLines());
			
			foreach (var element in localizableAttrs) {
				localizations.AppendFormat("<String Id=\"{0}\" Overridable=\"yes\">{1}</String>",element.Key,element.Value.Value.TokenizeXml());
				element.Value.Value = String.Format("!(loc.{0})", element.Key);
			}
			
			for (int i = 0; i < uiPart.ChildNodes.Count; ) {
				string name = uiPart.ChildNodes[i].Name;
				string nodeText = uiPart.ChildNodes[i].OuterXml;
				int nodesCount = uiPart.ChildNodes.Count;
				if (uiPart.ChildNodes[i].Name == "Error" || uiPart.ChildNodes[i].Name == "UIText" || uiPart.ChildNodes[i].Name == "ProgressText")
					uiPart.RemoveChild(uiPart.ChildNodes[i]);
				else
					i++;
				nodesCount = uiPart.ChildNodes.Count;
			}
			
			sb = new StringBuilder();
			foreach (var element in UiProperties) {
				if (element.Value != null) {
					sb.Append(element.Value.OuterXml);
					element.Value.ParentNode.RemoveChild(element.Value);
				}
			}
			foreach (var element in UiBinaries) {
				if (!Directory.Exists("UI")) Directory.CreateDirectory("UI");
				if(element.Value != null) {
					File.Copy(SourcePath + element.Value.Attributes["SourceFile"].Value,element.Value.Attributes["SourceFile"].Value.Replace(".\\Binary\\",".\\UI\\"),true);
					sb.Append(element.Value.OuterXml.Replace(".\\Binary\\",".\\UI\\"));
					element.Value.ParentNode.RemoveChild(element.Value);
				}
			}
			string propsandbins = sb.ToString().RemoveWixNS();
			
			File.WriteAllText("en-us.wxl", String.Format("<?xml version=\"1.0\" encoding=\"utf-8\"?><WixLocalization Culture=\"en-us\" Codepage=\"1252\" xmlns=\"http://schemas.microsoft.com/wix/2006/localization\">{0}</WixLocalization>",localizations.ToString()).SeparateLines());
			File.WriteAllText("UI.wxs", String.Format(fragmentTemplate, (propsandbins + uiPart.OuterXml).RemoveWixNS().Replace("<UI>","<UI><UIRef Id=\"ProgressText\"/><UIRef Id=\"UIText\"/>")).SeparateLines());
			uiPart.ParentNode.RemoveChild(uiPart);
		}
	}
}
