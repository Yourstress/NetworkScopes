using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NetworkScopes.CodeGeneration
{
	public class NetworkScopeProcessor
	{
		[InitializeOnLoadMethod]
		public static void GenerateNetworkScopes()
		{
			ClearLog();

			GenerateNetworkScopes(!ScopeGenerationConfig.AutoGenerateScopeClasses);
		}

		public static void GenerateNetworkScopes(bool logOnly)
		{
			SerializationProvider serializer = new SerializationProvider();

			// log/generate marked scopes
			foreach (ScopeDefinition scopeGen in NetworkScopeUtility.FindScopeGenerationConfigs(serializer))
			{
				ScriptWriter writer = scopeGen.scopeDefinition.ToScriptWriter();

				if (logOnly)
					Debug.Log(writer);
				else
				{
					string scriptWritePath = scopeGen.GetScopeScriptPath();

					File.WriteAllText(scriptWritePath, writer.ToString());
				}
			}

			// log types that failed to serialize within the previous block
			foreach (KeyValuePair<Type, SerializationFailureReason> kvp in serializer.failedTypes)
			{
				switch (kvp.Value)
				{
					case SerializationFailureReason.TypeNotSerializable:
						Debug.LogWarningFormat(
							"The type <b>{0}</b> can not be serialized because it does not implement <b>ISerializable</b>.", kvp.Key.Name);
						break;
//					case SerializationFailureReason.TypeSerializationPendingGeneration:
//						Debug.LogWarningFormat(
//							"The type <b>{0}</b> is marked for serialization but is pending code generation. Use the <b>Tools/Network Scopes/Generate Scopes</b> menu item to generate the serialization methods.",
//							kvp.Key.Name);
//						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}


			if (!logOnly)
			{
				serializer.GenerateTypeSerializers();

				AssetDatabase.Refresh();
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