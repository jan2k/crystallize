using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace crystallize
{
	interface IParser
	{
		string Name();
		string XPath();
		void Parse(XmlNode part);
		void Parse(XmlNodeList part);
		void Process();
	}
}
