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
		public static List<ScopeDefinition> FindScopeDefinitions(SerializationProvider serializer)
		{
			List<ScopeDefinition> scopes = new List<ScopeDefinition>();

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			foreach (Type t in assembly.GetTypes())
			{
				// skip non-interfaces
				if (!t.IsInterface || t.BaseType != null)
					continue;

				ScopeAttribute scopeAttr = GetCustomAttribute<ScopeAttribute>(t);

				if (scopeAttr == null)
					continue;

				// validate name
				if (t.Name[0] != 'I')
				{
					Debug.LogErrorFormat("The scope definition <color=red>{0}</color> must be renamed to I{0}.", t.Name);
					continue;
				}

				scopes.Add(new ScopeDefinition(t, scopeAttr, scopeAttr.entityType, serializer));
			}

			return scopes;
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