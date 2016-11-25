
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
							ScopeDefinition scope = ScopeDefinition.NewAuthenticatorScope(t, interfaceType);
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
				string scopePath = MakeScopePath(scope.scopeType);
				scope.WriteToFile(scopePath, false, true);

				if (!scope.HasRuntimeConcreteType)
					scope.CreateConcreteClassDefinition().WriteToFile(scopePath, false, false);
			}
		}

		public void Print()
		{
			foreach (ScopeDefinition scope in abstractScopes)
			{
				Debug.Log(scope);
			}
		}

		private static string MakeScopePath(Type scopeType)
		{
			string[] foundAssets = AssetDatabase.FindAssets(scopeType.Name);

			if (foundAssets.Length > 0)
			{
				string scopeName = scopeType.Name;

				for (int x = 0; x < foundAssets.Length; x++)
				{
					string path = AssetDatabase.GUIDToAssetPath(foundAssets[x]);
					if (Path.GetFileNameWithoutExtension(path) == scopeName)
						return Path.GetDirectoryName(path);
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