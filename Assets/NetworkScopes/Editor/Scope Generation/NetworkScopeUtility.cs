using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NetworkScopes.CodeGeneration
{
	public static class NetworkScopeUtility
	{
		public static List<ScopeDefinition> FindScopeGenerationConfigs(SerializationProvider serializer)
		{
			List<ScopeDefinition> scopes = new List<ScopeDefinition>();

			FindScopeGenerationConfigs(scopes, serializer, typeof(IServerScope), typeof(IClientScope), true);
			FindScopeGenerationConfigs(scopes, serializer, typeof(IClientScope), typeof(IServerScope), false);

			return scopes;
		}

		private static void FindScopeGenerationConfigs(List<ScopeDefinition> scopes, SerializationProvider serializer, Type interfaceType, Type otherScopeInterface, bool isServerScope)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			foreach (Type t in assembly.GetTypes())
			{
				// skip non-interfaces or the exact interface - we only want implementors of it
				if (!t.IsInterface || t == interfaceType || !interfaceType.IsAssignableFrom(t))
					continue;

				ScopeAttribute scopeAttr = GetCustomAttribute<ScopeAttribute>(t);

				// no [Scope] attribute? warn
				if (scopeAttr == null)
				{
					Debug.LogWarningFormat("The scope definition interface <color=red>{0}</color> does not have a [Scope] attribute. Attach one to define the other scope.", t);
					continue;
				}

				// [Scope] attribute has wrong type? error
				Type otherScopeDefType = scopeAttr.otherScopeType;
				if (!otherScopeDefType.IsInterface || otherScopeDefType == otherScopeInterface)
				{
					Debug.LogErrorFormat("The scope <color=red>{0}</color> can not be linked to type {1} because it does not implement {2}", t, otherScopeDefType, otherScopeInterface);
					continue;
				}

				// validate name
				if (t.Name[0] != 'I')
				{
					Debug.LogErrorFormat("The scope definition <color=red>{0}</color> must be renamed to I{0}.", t.Name);
					continue;
				}

				scopes.Add(new ScopeDefinition(t, scopeAttr, isServerScope, serializer));
			}
		}

		public static T GetCustomAttribute<T>(this Type type)
		{
			object[] attrs = type.GetCustomAttributes(typeof(T), true);
			if (attrs.Length > 0)
				return (T) attrs[0];

			return default(T);
		}
	}
}