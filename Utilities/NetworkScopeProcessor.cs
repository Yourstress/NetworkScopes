using System;
using System.IO;
using CodeGeneration;

namespace NetworkScopes
{
	public class NetworkScopeProcessor
	{
		public readonly Type type;
		public readonly ScopeAttribute injectScopeAttribute;
		public readonly ClassDefinition generatedClass;

		public readonly string preGeneratedContents;
		public readonly string postGeneratedContents;

		public readonly string Name;
		public readonly bool areFilesMatching;

		public NetworkScopeProcessor(Type t, ScopeAttribute injectScopeAttr)
		{
			type = t;
			injectScopeAttribute = injectScopeAttr;

			generatedClass = new ClassDefinition(type.Name, type.Namespace, true);;
			generatedClass.FileName = "G-" + type.Name;

			// process/generate class
			injectScopeAttribute.ProcessClass(type, generatedClass);
			generatedClass.AddMethodImports();

			// load contents
			string filePath = NetworkScopePostProcessor.GetGeneratedClassPath(generatedClass, true);

			preGeneratedContents = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
			postGeneratedContents = generatedClass.ToString();

			areFilesMatching = preGeneratedContents == postGeneratedContents;

			Name = t.Name;
		}

		public static implicit operator Type(NetworkScopeProcessor netScopeProcessor)
		{
			return netScopeProcessor.type;
		}

		public bool WriteToFile()
		{
			if (areFilesMatching)
				return false;

			return WriteClass(generatedClass);
		}

		public static bool WriteClass(ClassDefinition classDef)
		{
			string path = NetworkScopePostProcessor.GetGeneratedClassPath(classDef, true);

			string preGenContents = File.Exists(path) ? File.ReadAllText(path) : "";
			string postGenContents = classDef.ToString();

			bool didSave = false;

			if (preGenContents != postGenContents)
			{
				File.WriteAllText(path, postGenContents);
				didSave = true;
			}

			// write all dependencies
			for (int x = 0; x < classDef.dependancies.Count; x++)
			{
				WriteClass(classDef.dependancies[x]);
			}

			return didSave;
		}
	}
}