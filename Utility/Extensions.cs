using System;
using System.Xml;

namespace crystallize.Utility
{
	/// <summary>
	/// extension methods
	/// </summary>
	public static class Extensions
	{
		public static string SeparateLines(this String str)
		{
			return str.Replace("><",">\r\n<");
		}
		public static string RemoveWixNS(this String str)
		{
			return str.Replace(" xmlns=\"http://schemas.microsoft.com/wix/2006/wi\"","");
		}
		public static string TokenizeXml(this String str)
		{
			return str.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");
		}
		
		public static void AddAttribute(this XmlNode node, string AttrName, string AttrVal)
		{
			XmlAttribute attr = node.OwnerDocument.CreateAttribute(AttrName);
			attr.Value = AttrVal;
			node.Attributes.Append(attr);
		}
	}
}
