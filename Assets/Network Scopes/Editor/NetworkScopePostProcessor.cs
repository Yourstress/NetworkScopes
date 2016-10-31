
using System;
using UnityEngine;
using UnityEditor;
using CodeGeneration;
using System.Reflection;
using System.IO;

public class NetworkScopePostProcessor
{
	[InitializeOnLoadMethod]
	static void GenerateNetworkClasses()
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			// skip anything other than the main assembly
			if (!assembly.FullName.StartsWith("Assembly-CSharp,"))
				continue;
			
			foreach (Type t in assembly.GetTypes())
				ProcessType(t);
		}

		if (NetworkScopeUtility.SerializerClass.classes.Count > 0)
		{
			string path = Path.Combine(Application.dataPath, "GeneratedCode");
			path = Path.Combine(path, "Serializers.cs");

			File.WriteAllText(path, NetworkScopeUtility.SerializerClass.ToString());
		}

		AssetDatabase.Refresh();
	}

	static void ProcessType(Type type)
	{
		object[] attrs = type.GetCustomAttributes(false);

		if (attrs.Length == 0)
			return;

		ClassDefinition classDef = null;

		for (int x = 0; x < attrs.Length; x++)
		{
			if (attrs[x].GetType().IsSubclassOf(typeof(InjectAttribute)))
			{
				if (classDef == null)
					classDef = new ClassDefinition(type.Name, type.Namespace, true);

				((InjectAttribute)attrs[x]).ProcessClass(type, classDef);
			}
		}

		if (classDef != null)
		{
			string path = Path.Combine(Application.dataPath, "GeneratedCode");

			Directory.CreateDirectory(path);

			// group scripts by namespaces
			if (!string.IsNullOrEmpty(classDef.Namespace))
			{
				if (classDef.Namespace.Contains("."))
					path = Path.Combine(path, classDef.Namespace.Substring(0, classDef.Namespace.IndexOf(".")));
				else
					path = Path.Combine(path, classDef.Namespace);

				Directory.CreateDirectory(path);
			}

			path = Path.Combine(path, string.Format("{0}.cs", classDef.Name));

			File.WriteAllText(path, classDef.ToString());

		}
	}
}
