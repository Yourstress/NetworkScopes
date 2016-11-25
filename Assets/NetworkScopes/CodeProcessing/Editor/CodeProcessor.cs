
namespace NetworkScopes.CodeProcessing
{
	using UnityEngine;
	using UnityEngine.Assertions;
	using UnityEditor;
	using System;
	using System.Reflection;
	using System.Collections.Generic;
	using System.Text;
	using System.IO;


	public class CodeProcessor
	{
		public List<ScopeDefinition> abstractScopes = new List<ScopeDefinition>();

		public CodeProcessor()
		{
			Type serverScopeType = typeof(IServerScope<>);
			Type clientScopeType = typeof(IClientScope);
			Type authenticatorInterfaceType = typeof(IAuthenticator);
			Type authenticatorBaseType = typeof(BaseAuthenticator);

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				// skip anything other than the main assembly
				if (!assembly.FullName.StartsWith("Assembly-CSharp,"))
					continue;

				foreach (Type t in assembly.GetTypes())
				{
					// skip processing generated types
					if (t.GetCustomAttributes(typeof(GeneratedAttribute), false).Length > 0)
						continue;
					
					// find server and client scope interfaces and generate scope classes that match their "template"
					Type[] interfaces = t.GetInterfaces();
					for (int i = 0; i < interfaces.Length; i++)
					{
						Type interfaceType = interfaces[i];

						if (interfaceType == clientScopeType)
						{
							ScopeDefinition scope = ScopeDefinition.NewClientScopeWriter(t);
							abstractScopes.Add(scope);
						}
						else if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == serverScopeType)
						{
							ScopeDefinition scope = ScopeDefinition.NewServerScopeWriter(t, interfaceType);
							abstractScopes.Add(scope);
						}
						else if (interfaceType == authenticatorInterfaceType && t != authenticatorBaseType)
						{
							ScopeDefinition scope = ScopeDefinition.NewAuthenticatorScope(t);
							abstractScopes.Add(scope);
						}
					}
				}
			}
		}

		[MenuItem("Network Scopes/Generate Code")]
		public static void GenerateCode()
		{
			CodeProcessor processor = new CodeProcessor();
			processor.Generate();

			AssetDatabase.Refresh();
		}


		[MenuItem("Network Scopes/Print Preview Code")]
		[UnityEditor.Callbacks.DidReloadScripts]
		public static void PrintPreviewCode()
		{
			ClearLog();

			CodeProcessor processor = new CodeProcessor();
			processor.Print();
		}

		public void Generate()
		{
			foreach (ScopeDefinition scope in abstractScopes)
			{
				string path = MakeScopePath(scope.scopeType, true);
				scope.WriteToFile(path, false, true);

				if (!scope.HasRuntimeConcreteType)
				{
					path = MakeScopePath(scope.scopeType, true);
					scope.CreateConcreteClassDefinition().WriteToFile(path, false, false);
				}
			}
		}

		public void Print()
		{
			StringBuilder sb = new StringBuilder();
			foreach (ScopeDefinition abstractDef in abstractScopes)
			{
				Print("ABSTRACT", abstractDef, sb);

				ClassDefinition concreteScopeDef = abstractDef.CreateConcreteClassDefinition();
				Print("<color=green>CONCRETE</color>", concreteScopeDef, sb);
			}
		}

		private void Print(string prefix, ClassDefinition classDef, StringBuilder sb)
		{
			sb.AppendFormat("{0} <color=white>{1}</color>", prefix, classDef.Name);
			sb.AppendLine();
			sb.AppendLine(classDef.ToString());

			Debug.Log(sb.ToString());

			sb.Remove(0, sb.Length);
		}

		private static string MakeScopePath(Type scopeType, bool putInGeneratedFolder)
		{
			string[] foundAssets = AssetDatabase.FindAssets(scopeType.Name);

			if (foundAssets.Length > 0)
			{
				string scopeName = scopeType.Name;

				for (int x = 0; x < foundAssets.Length; x++)
				{
					string path = AssetDatabase.GUIDToAssetPath(foundAssets[x]);
					if (Path.GetFileNameWithoutExtension(path) == scopeName)
					{
						// trim the file name
						path = Path.GetDirectoryName(path);

						if (putInGeneratedFolder)
							path = Path.Combine(path, "Generated");
					}
				}
			}
			return "Assets/GeneratedCode";
		}
		
		public static void ClearLog()
		{
			var assembly = Assembly.GetAssembly(typeof(UnityEditor.ActiveEditorTracker));
			var type = assembly.GetType("UnityEditorInternal.LogEntries");
			var method = type.GetMethod("Clear");
			method.Invoke(new object(), null);
		}
	}
}