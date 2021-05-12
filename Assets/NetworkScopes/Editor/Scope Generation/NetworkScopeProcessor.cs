

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace NetworkScopes.CodeGeneration
{
	public class NetworkScopeProcessor
	{
		public static void GenerateNetworkScopes(bool logOnly)
		{
			SerializationProvider serializer = new SerializationProvider();

			List<ScopeDefinition> scopes = NetworkScopeUtility.FindScopeDefinitions(serializer);
			
			GenerateNetworkScopes(serializer, scopes, logOnly);
		}

		public static void GenerateNetworkScopes(SerializationProvider serializer, List<ScopeDefinition> scopes, bool logOnly)
		{
			// log types that failed to serialize within the previous block
			foreach (KeyValuePair<Type, SerializationFailureReason> kvp in serializer.failedTypes)
			{
				switch (kvp.Value)
				{
					case SerializationFailureReason.TypeNotSerializable:
						NSDebug.LogWarning(
							$"The type <b>{kvp.Key.Name}</b> can not be serialized because it does not implement <b>ISerializable</b>. Use the <b>[NetworkSerialize]</b> to generate serialization code or implement <b>ISerializable</b> to manually serialize it.");
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

				#if UNITY_EDITOR
				AssetDatabase.Refresh();
				#endif
			}
		}

		private static void WriteScope(ScopeDefinition scope, bool logOnly)
		{
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

					// write concrete class
					WriteClass(blankConcreteScope, scope.GetConcreteScriptPath(), false, logOnly);
				}
			}

			// write abstract class
			WriteClass(scope.scopeDefinition, scope.GetAbstractScriptPath(), true, logOnly);
		}

		private static void WriteClass(ClassDefinition classDef, string path, bool overwriteIfExists, bool logOnly)
		{
			ScriptWriter writer = classDef.ToScriptWriter();

			if (logOnly)
			{
				// write scope name first when logging
				writer.WriteAt(0, classDef.type.Name+Environment.NewLine);

				
				NSDebug.Log($"Writing class {classDef.type.Name} to {path}");
				NSDebug.Log(writer.ToString());
				
			}
			else
			{
				if (overwriteIfExists || !File.Exists(path))
					File.WriteAllText(path, writer.ToString());
			}
		}

		#if UNITY_EDITOR
		private const string menuItem_AutoGenScopes = "Tools/Network Scopes X/Auto Generate Scope Classes";
		private const string menuItem_GenerateScopes = "Tools/Network Scopes X/Generate Scopes";
		private const string menuItem_LogScopes = "Tools/Network Scopes X/Generate Scopes (Log only)";
		
		[MenuItem(menuItem_AutoGenScopes, false, 500)]
		static void Menu_AutoGenerateScopes()
		{
			ScopeGenerationConfig.AutoGenerateScopeClasses = !ScopeGenerationConfig.AutoGenerateScopeClasses;
		}

		[MenuItem(menuItem_AutoGenScopes, true)]
		static bool Menu_AutoGenerateScopes_Validate()
		{
			Menu.SetChecked(menuItem_AutoGenScopes, ScopeGenerationConfig.AutoGenerateScopeClasses);
			return true;
		}

		[MenuItem(menuItem_GenerateScopes, false, 502)]
		static void Menu_GenerateScopesNow()
		{
			GenerateNetworkScopes(false);
		}

		[MenuItem(menuItem_LogScopes, false, 501)]
		static void Menu_LogScopesNow()
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
		#endif
	}
}
