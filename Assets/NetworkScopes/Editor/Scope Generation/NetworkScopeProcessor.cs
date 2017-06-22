using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NetworkScopes.CodeGeneration
{
	public class NetworkScopeProcessor
	{
//		[InitializeOnLoadMethod]
//		public static void GenerateNetworkScopes()
//		{
//			ClearLog();
//
//			GenerateNetworkScopes(!ScopeGenerationConfig.AutoGenerateScopeClasses);
//		}

		public static void GenerateNetworkScopes(bool logOnly)
		{
			SerializationProvider serializer = new SerializationProvider();

			List<ScopeDefinition> scopes = NetworkScopeUtility.FindScopeDefinitions(serializer);

			// log types that failed to serialize within the previous block
			foreach (KeyValuePair<Type, SerializationFailureReason> kvp in serializer.failedTypes)
			{
				switch (kvp.Value)
				{
					case SerializationFailureReason.TypeNotSerializable:
						Debug.LogWarningFormat(
							"The type <b>{0}</b> can not be serialized because it does not implement <b>ISerializable</b>. Use the <b>[NetworkSerialize]</b> to generate serialization code or implement <b>ISerializable</b> to manually serialize it.", kvp.Key.Name);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			if (!logOnly && serializer.failedTypes.Count > 0)
				throw new Exception("Failed to generate scopes. Use the <b>[NetworkSerializable]</b> attribute to generate serialization code, or Implement <b>ISerializable]</b> to manually serialize your types.");

			// log/generate marked scopes
			foreach (ScopeDefinition scope in scopes)
			{
				WriteScope(scope, logOnly);
			}


			if (!logOnly)
			{
				serializer.GenerateTypeSerializers();

				AssetDatabase.Refresh();
			}
		}

		private static void WriteScope(ScopeDefinition scope, bool logOnly)
		{
			string scriptWritePath = scope.GetScopeScriptPath();

			// if this is an abstract scope, generate a blank class override if one doesn't already exist
			if (scope.scopeDefinition.isAbstract)
			{
				string concreteTypeName = scope.scopeDefinition.type.Name.Replace("_Abstract", string.Empty);
				Type concreteType = Type.GetType(concreteTypeName, false);
				bool classExists = concreteType != null;

				if (!classExists)
				{
					ClassDefinition blankConcreteScope = new ClassDefinition(concreteTypeName, scope.scopeDefinition.type.Namespace);
					blankConcreteScope.baseType = scope.scopeDefinition.type;
					blankConcreteScope.ResolveImportType(scope.scopeDefinition.type);

					// add interface methods with override modifier
					var overrideMethods = scope.scopeInterface.GetMethods().Select(m =>
					{
						MethodDefinition md = new MethodDefinition(m);
						md.Parameters.AddRange(m.GetParameters().Select(p => new ParameterDefinition(p.Name, p.ParameterType)).ToArray());
						md.ReturnType = m.ReturnType;
						md.AccessModifier = AccessModifier.Protected;
						md.MethodModifier = MethodModifier.Override;
						md.Body.AddNotImplementedException();
						return md;
					});
					blankConcreteScope.methods.AddRange(overrideMethods);

					string concreteTypePath = ScopeDefinition.MakeScopeScriptPath(concreteTypeName, scope.scopeInterface.Name);

					if (!File.Exists(concreteTypeName))
						WriteClass(blankConcreteScope, concreteTypePath, logOnly);
				}
			}

			WriteClass(scope.scopeDefinition, scriptWritePath, logOnly);
		}

		private static void WriteClass(ClassDefinition classDef, string path, bool logOnly)
		{
			ScriptWriter writer = classDef.ToScriptWriter();

			if (logOnly)
			{
				// write scope name first when logging
				writer.WriteAt(0, classDef.type.Name+Environment.NewLine);

				Debug.Log(writer);
			}
			else
			{
				File.WriteAllText(path, writer.ToString());
			}
		}

		private const string menuItem_AutoGenScopes = "Tools/Network Scopes/Auto Generate Scope Classes";
		private const string menuItem_GenerateScopes = "Tools/Network Scopes/Generate Scopes";
		private const string menuItem_LogScopes = "Tools/Network Scopes/Generate Scopes (Log only)";

		[MenuItem(menuItem_AutoGenScopes, false, 500)]
		public static void Menu_AutoGenerateScopes()
		{
			ScopeGenerationConfig.AutoGenerateScopeClasses = !ScopeGenerationConfig.AutoGenerateScopeClasses;
		}

		[MenuItem(menuItem_AutoGenScopes, true)]
		public static bool Menu_AutoGenerateScopes_Validate()
		{
			Menu.SetChecked(menuItem_AutoGenScopes, ScopeGenerationConfig.AutoGenerateScopeClasses);
			return true;
		}

		[MenuItem(menuItem_GenerateScopes, false, 502)]
		public static void Menu_GenerateScopesNow()
		{
			GenerateNetworkScopes(false);
		}

		[MenuItem(menuItem_LogScopes, false, 501)]
		public static void Menu_LogScopesNow()
		{
			GenerateNetworkScopes(true);
		}

		public static void ClearLog()
		{
			var assembly = Assembly.GetAssembly(typeof(ActiveEditorTracker));
			var type = assembly.GetType("UnityEditorInternal.LogEntries");
			var method = type.GetMethod("Clear");
			method.Invoke(new object(), null);
		}
	}
}