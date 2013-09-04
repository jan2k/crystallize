using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace crystallize.Parsers
{
	class SequencesParser : BaseParser
	{
		override public string Name() { return "Sequences"; }
		override public string XPath() { return ""; } //select nodes by self

		SortedDictionary<int, XmlNode> insUIseq = new SortedDictionary<int, XmlNode>();
		SortedDictionary<int, XmlNode> insExeseq = new SortedDictionary<int, XmlNode>();
		SortedDictionary<int, XmlNode> admUIseq = new SortedDictionary<int, XmlNode>();
		SortedDictionary<int, XmlNode> admExeseq = new SortedDictionary<int, XmlNode>();
		SortedDictionary<int, XmlNode> advUIseq = new SortedDictionary<int, XmlNode>();
		SortedDictionary<int, XmlNode> advExeseq = new SortedDictionary<int, XmlNode>();
		
		List<string> standardActions = new List<string>(new string[] {"AllocateRegistrySpace", "AppSearch", "BindImage", "CCPSearch", "CostFinalize", "CostInitialize", "CreateFolders", "CreateShortcuts", "DeleteServices", "DisableRollback", "DuplicateFiles", "ExecuteAction", "FileCost", "FindRelatedProducts", "ForceReboot", "InstallAdminPackage", "InstallExecute", "InstallFiles", "InstallFinalize", "InstallInitialize", "InstallSFPCatalogFile", "InstallValidate", "IsolateComponents", "LaunchConditions", "MigrateFeatureStates", "MoveFiles", "MsiConfigureServices", "MsiPublishAssemblies", "MsiUnpublishAssemblies", "InstallODBC", "InstallServices", "PatchFiles", "ProcessComponents", "PublishComponents", "PublishFeatures", "PublishProduct", "RegisterClassInfo", "RegisterComPlus", "RegisterExtensionInfo", "RegisterFonts", "RegisterMIMEInfo", "RegisterProduct", "RegisterProgIdInfo", "RegisterTypeLibraries", "RegisterUser", "RemoveDuplicateFiles", "RemoveEnvironmentStrings", "RemoveExistingProducts", "RemoveFiles", "RemoveFolders", "RemoveIniValues", "RemoveODBC", "RemoveRegistryValues", "RemoveShortcuts", "ResolveSource", "RMCCPSearch", "ScheduleReboot", "SelfRegModules", "SelfUnregModules", "SetODBCFolders", "StartServices", "StopServices", "UnpublishComponents", "UnpublishFeatures", "UnregisterClassInfo", "UnregisterComPlus", "UnregisterExtensionInfo", "UnregisterFonts", "UnregisterMIMEInfo", "UnregisterProgIdInfo", "UnregisterTypeLibraries", "ValidateProductID", "WriteEnvironmentStrings", "WriteIniValues", "WriteRegistryValues"});
		
		override public void Parse(XmlNode part)
		{
			foreach (XmlNode action in GetNodes(part, "//wixns:InstallUISequence/*")) {
				if (action.Attributes["Sequence"] != null) insUIseq.Add(UInt16.Parse(action.Attributes["Sequence"].Value),action);
			}
			foreach (XmlNode action in GetNodes(part, "//wixns:InstallExecuteSequence/*")) {
				if (action.Attributes["Sequence"] != null) insExeseq.Add(UInt16.Parse(action.Attributes["Sequence"].Value),action);
			}
			foreach (XmlNode action in GetNodes(part, "//wixns:AdminUISequence/*")) {
				if (action.Attributes["Sequence"] != null) admUIseq.Add(UInt16.Parse(action.Attributes["Sequence"].Value),action);
			}
			foreach (XmlNode action in GetNodes(part, "//wixns:AdminExecuteSequence/*")) {
				if (action.Attributes["Sequence"] != null) admExeseq.Add(UInt16.Parse(action.Attributes["Sequence"].Value),action);
			}
			foreach (XmlNode action in GetNodes(part, "//wixns:AdvtUISequence/*")) {
				if (action.Attributes["Sequence"] != null) advUIseq.Add(UInt16.Parse(action.Attributes["Sequence"].Value),action);
			}
			foreach (XmlNode action in GetNodes(part, "//wixns:AdvtExecuteSequence/*")) {
				if (action.Attributes["Sequence"] != null) advExeseq.Add(UInt16.Parse(action.Attributes["Sequence"].Value),action);
			}
		}

		public override void Process()
		{
			ProcessSeq (insUIseq, insExeseq);
			ProcessSeq (admUIseq, admExeseq);
			ProcessSeq (advUIseq, advExeseq);
		}
		
		void ProcessSeq(SortedDictionary<int, XmlNode> uiSeq, SortedDictionary<int, XmlNode> exeSeq)
		{
			string prev = null;
			foreach (var element in uiSeq) {
				if (prev != null) {
					XmlAttribute afterAttr = element.Value.OwnerDocument.CreateAttribute("After");
					afterAttr.Value = prev;
					element.Value.Attributes.Append(afterAttr);
					element.Value.Attributes.RemoveNamedItem("Sequence");
				}
				if (element.Value.Name != "Custom" && element.Value.Name != "Show"){
					if (standardActions.IndexOf(element.Value.Name) > -1) {
						element.Value.ParentNode.RemoveChild(element.Value);
					}
					prev = element.Value.Name;
				}
			}
			prev = null;
			foreach (var element in exeSeq) {
				if (prev != null) {
					XmlAttribute afterAttr = element.Value.OwnerDocument.CreateAttribute("After");
					afterAttr.Value = prev;
					element.Value.Attributes.Append(afterAttr);
					element.Value.Attributes.RemoveNamedItem("Sequence");
				}
				if (element.Value.Name != "Custom"){
					if (standardActions.IndexOf(element.Value.Name) > -1) {
						element.Value.ParentNode.RemoveChild(element.Value);
					}
					prev = element.Value.Name;
				}
			}
		}
	}
}
