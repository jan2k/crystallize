using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using crystallize.Utility;

namespace crystallize.Parsers
{
	class FilesParser : BaseParser
	{
		DirectoryNode targetDir = new DirectoryNode();

		override public string Name() { return "Files"; }
		override public string XPath() { return "//wixns:Directory"; }

		List<XmlNode> regComponents = new List<XmlNode>();
		public Dictionary<XmlNode, string> Components;

		IParser registryParser = new RegistryParser();

		override public void Parse(XmlNode part)
		{
			if (part.Attributes["Id"].Value != "TARGETDIR") return;
			targetDir.xmlNode = part;

			foreach (XmlNode node in part.ChildNodes)
				Parse(node, targetDir);
		}

		void Parse(XmlNode part, DirectoryNode parentFolder)
		{
			if (part.Name == "Directory") {
				DirectoryNode dirNode = new DirectoryNode();
				parentFolder.childDirs.Add(dirNode);
				parentFolder = dirNode;
				dirNode.xmlNode = part;
			}
			if (part.Name == "File") {
				parentFolder.files.Add(part);
			}

			foreach (XmlNode node in part.ChildNodes)
				Parse(node, parentFolder);

			if (part.Name == "Component" && GetNodes(part, "wixns:File").Count == 0)
			{
				if (!part.InnerXml.Contains("CreateFolder")) {
					regComponents.Add(part);
				}
			}
		}

		override public void Process() {
			StringBuilder regPartHKLM = new StringBuilder();
			StringBuilder regPartHKCU = new StringBuilder();
			StringBuilder regPartHKCR = new StringBuilder();
			StringBuilder regPartHKMU = new StringBuilder();
			StringBuilder regPartBad = new StringBuilder();
			foreach(XmlNode regComponentNode in regComponents) {
				XmlNodeList keyElement = GetNodes(regComponentNode, "wixns:*[@KeyPath]");
				string regRoot = (keyElement.Count > 0) ? keyElement[0].Attributes["Root"].Value : "Bad";
				if (regRoot == "HKLM") regPartHKLM.AppendLine(regComponentNode.OuterXml);
				if (regRoot == "HKCU") regPartHKCU.AppendLine(regComponentNode.OuterXml);
				if (regRoot == "HKCR") regPartHKCR.AppendLine(regComponentNode.OuterXml);
				if (regRoot == "HKMU") regPartHKMU.AppendLine(regComponentNode.OuterXml);
				if (regRoot == "Bad") {regComponentNode.AddAttribute("KeyPath","yes");regPartBad.AppendLine(regComponentNode.OuterXml);}
				regComponentNode.ParentNode.RemoveChild(regComponentNode);
			}
			string regPart = regPartHKLM.ToString()+regPartHKCR.ToString()+regPartHKCU.ToString()+regPartHKMU.ToString()+regPartBad.ToString();
			
			directoriesReconstructor(targetDir,".");
			
			StringBuilder componentGroups = new StringBuilder();
			string featureId = String.Empty;
			foreach (var element in Components) {
				if (element.Value != featureId) {
					if (componentGroups.Length != 0) componentGroups.Append("</ComponentGroup>");
					componentGroups.AppendFormat("<ComponentGroup Id=\"{0}\">", element.Value);
					featureId = element.Value;
				}
				componentGroups.Append(element.Key.OuterXml);
			}
			componentGroups.Append("</ComponentGroup>");
			
			File.WriteAllText("Files.wxs", String.Format(fragmentTemplate,
			                                             ("<DirectoryRef Id=\"TARGETDIR\">" + targetDir.xmlNode.InnerXml + "</DirectoryRef>"
			                                              + componentGroups.ToString()
			                                             ).RemoveWixNS()).SeparateLines());

			File.WriteAllText("Registry.wxs", String.Format(fragmentTemplate,
			                                                "<DirectoryRef Id=\"TARGETDIR\">"+regPart.RemoveWixNS()+"</DirectoryRef>").SeparateLines());
			
			targetDir.xmlNode.InnerXml = String.Empty;
		}

		void directoriesReconstructor(DirectoryNode curentNode, string curPath)
		{
			if (curentNode.xmlNode.Attributes["Name"] != null)
				curPath += @"\"+curentNode.xmlNode.Attributes["Name"].Value;
			else
				curPath += @"\"+curentNode.xmlNode.Attributes["Id"].Value;
			if (!Directory.Exists(curPath)) Directory.CreateDirectory(curPath);
			if (curentNode.files.Count > 0) {
				XmlAttribute fileSourceAttr = curentNode.xmlNode.OwnerDocument.CreateAttribute("FileSource");
				fileSourceAttr.Value = curPath;
				curentNode.xmlNode.Attributes.Append(fileSourceAttr);
			}
			foreach (XmlNode fileElement in curentNode.files) {
				if (fileElement.Attributes["Vital"] != null)
					if (fileElement.Attributes["Vital"].Value == "no") fileElement.Attributes.RemoveNamedItem("Vital");
				if (fileElement.Attributes["DiskId"] != null)
					if (fileElement.Attributes["DiskId"].Value == "1") fileElement.Attributes.RemoveNamedItem("DiskId");
				if (fileElement.Attributes["KeyPath"] != null)
					if (fileElement.Attributes["KeyPath"].Value == "no") fileElement.Attributes.RemoveNamedItem("KeyPath");
				//Logger.Log("File copy: "+SourcePath+fileElement.Attributes["Source"].Value +" --> "+ curPath + "\\" + fileElement.Attributes["Name"].Value);
				File.Copy(SourcePath+fileElement.Attributes["Source"].Value, curPath + "\\" + fileElement.Attributes["Name"].Value, true);
				if (fileElement.Attributes["Source"] != null) fileElement.Attributes.RemoveNamedItem("Source");
			}
			foreach (var childDir in curentNode.childDirs) {
				directoriesReconstructor(childDir, curPath);
			}
		}
	}

	class DirectoryNode
	{
		public XmlNode xmlNode;
		public List<XmlNode> files = new List<XmlNode>();
		public List<DirectoryNode> childDirs = new List<DirectoryNode>();
	}
}
