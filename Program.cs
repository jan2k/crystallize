using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using crystallize.Parsers;

namespace crystallize
{
	class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length == 0) return Help();
			if (File.Exists(args[0]))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(args[0]);
				IParser mainParser = new MainParser();
				BaseParser.SourcePath = new FileInfo(args[0]).DirectoryName;
				mainParser.Parse(doc.DocumentElement);
				mainParser.Process();
				doc.Save("Setup.wxs");
				return 0;
			}
			return -1;
		}
		public static int Help()
		{
			Console.WriteLine("Usage: crystallize <filename>.wxs");
			return 1;
		}
	}
}