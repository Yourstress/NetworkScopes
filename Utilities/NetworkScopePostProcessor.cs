
#define DONT_WRITE_FILES
#define LOG_ALL_FILES
#define LOG_CLASS_BY_NAME

using System;
using CodeGeneration;
using System.Reflection;
using System.IO;
using NetworkScopes;

using NetworkScopes.CodeProcessors;

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
#endif

public class NetworkScopeConstants
{
	public static string rootPath => Application.dataPath;
}

public static class NetworkScopePostProcessor
{
#if LOG_CLASS_BY_NAME
	private const string LogClassName = "MyClass";
#endif

#if UNITY_EDITOR
//	[InitializeOnLoadMethod]
	[MenuItem("Network Scopes/Generate Code")]
	static void GenerateCode()
	{
		GenerateNetworkScopes(Application.dataPath);
	}
#endif

	public static void GenerateNetworkScopes(string path)
	{
		GenerateAuthenticationCode();
		GenerateNetworkClasses<ScopeAttribute>(NetworkScopeUtility.SerializerClass);
	}

	static void GenerateAuthenticationCode()
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (Type t in assembly.GetTypes())
			{
				// find all MasterServers and MasterClients
				if (t.BaseType != null)
				{
					if (t.BaseType == typeof(MasterClient))
						AuthenticationProcessor.ProcessClient(t);
//					if (t.BaseType.IsGenericType &&
//					    t.BaseType.GetGenericTypeDefinition() == typeof(MasterServer<>))
//						AuthenticationProcessor.ProcessServer(t);
				}
			}
		}
	}

	static void GenerateNetworkClasses<TInjectAttribute>(ClassDefinition serializerClass)
		where TInjectAttribute : InjectAttribute
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (Type t in assembly.GetTypes())
				ProcessType<TInjectAttribute>(t);
		}

		if (serializerClass.classes.Count > 0)
		{
			string path = Path.Combine( NetworkScopeConstants.rootPath, "GeneratedCode");
			path = Path.Combine(path, "Serializers.cs");

#if !DONT_WRITE_FILES
			File.WriteAllText(path, serializerClass.ToString());
#endif
		}

	}

	public static void ProcessType(Type type, InjectAttribute injectAttribute)
	{
		ClassDefinition classDef = new ClassDefinition(type.Name, type.Namespace, true);;
		classDef.FileName = "G-" + type.Name;

		injectAttribute.ProcessClass(type, classDef);
	}

	static void ProcessType<TInjectAttribute>(Type type) where TInjectAttribute : InjectAttribute
	{
		object[] attrs = type.GetCustomAttributes(false);

		if (attrs.Length == 0)
			return;

		Type desiredAttrType = typeof(TInjectAttribute);

		ClassDefinition classDef = null;

		for (int x = 0; x < attrs.Length; x++)
		{
			if (attrs[x].GetType() == desiredAttrType)
			{
				if (classDef == null)
				{
					classDef = new ClassDefinition(type.Name, type.Namespace, true);
					classDef.FileName = "G-" + type.Name;
				}

				((InjectAttribute) attrs[x]).ProcessClass(type, classDef);
			}
		}

#if !DONT_WRITE_FILES
		if (classDef != null)
			WriteClass(classDef);
#endif
	}

	public static string GetGeneratedCodePath(bool createDirectory)
	{
		string path = Path.Combine(NetworkScopeConstants.rootPath, "GeneratedCode");

		if (createDirectory)
			Directory.CreateDirectory(path);
		return path;
	}

	public static string GetGeneratedClassPath(ClassDefinition classDef, bool createDirectory)
	{
		return GetGeneratedClassPath(classDef.Namespace, classDef.FileName, false, createDirectory);
	}

	public static string GetGeneratedClassPath(string classNamespace, string className, bool prefixGeneratedFile, bool createDirectory)
	{
		string path = GetGeneratedCodePath(createDirectory);

		if (path == null)
			return null;

		// group scripts by namespaces
		if (!string.IsNullOrEmpty(classNamespace))
		{
			if (classNamespace.Contains("."))
				path = Path.Combine(path, classNamespace.Substring(0, classNamespace.IndexOf(".")));
			else
				path = Path.Combine(path, classNamespace);

			Directory.CreateDirectory(path);
		}

		string prefix = prefixGeneratedFile ? "G-" : "";

		return Path.Combine(path, $"{prefix}{className}.cs");
	}

	
}