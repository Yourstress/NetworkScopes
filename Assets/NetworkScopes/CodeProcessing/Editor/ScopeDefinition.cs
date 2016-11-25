
namespace NetworkScopes.CodeProcessing
{
	using UnityEngine;
	using System.Reflection;
	using System;
	using System.Collections.Generic;

	public class ScopeDefinition : ClassDefinition
	{
		public bool HasRuntimeConcreteType { get { return scopeType.Assembly.GetType(FullName) != null; } }

		public static ScopeDefinition NewServerScopeWriter(Type scopeType, Type interfaceType)
		{
			ScopeDefinition serverScope = new ScopeDefinition(scopeType);
			serverScope.SetBaseClass(typeof(ServerScope<>), interfaceType);
			return serverScope;
		}

		public static ScopeDefinition NewClientScopeWriter(Type scopeType)
		{
			ScopeDefinition clientScope = new ScopeDefinition(scopeType);
			clientScope.SetBaseClass(typeof(ClientScope));
			return clientScope;
		}

		public static ScopeDefinition NewAuthenticatorScope(Type scopeType)
		{
			ScopeDefinition authScope = new ScopeDefinition(scopeType);
			authScope.SetBaseClass(typeof(BaseAuthenticator));
			authScope.IsInterface = false;
			authScope.IsAbstract = true;
			return authScope;
		}

		public Type scopeType { get; private set; }

		private ScopeDefinition(Type scopeType)
		{
			Name = MakeScopeName(scopeType);
			Namespace = scopeType.Namespace;

			IsAbstract = true;
			AddMethods(scopeType, true, false);

			this.scopeType = scopeType;
		}

		public ClassDefinition CreateConcreteClassDefinition()
		{
			// make sure the file doesn't already exist
			string concreteClassName = Name.Replace("Scope", string.Empty);

			// create scope concrete class if it doesn't exist
			ClassDefinition concreteScopeDef = new ClassDefinition(concreteClassName, Namespace);
			concreteScopeDef.BaseClass = Name;
			concreteScopeDef.AddMethods(scopeType, false, true);

			return concreteScopeDef;
		}

		private static string MakeScopeName(Type scopeType)
		{
			string name = scopeType.Name;

			// trim the 'I' from the class name
			if (name.StartsWith("I"))
				name = name.Substring(1, name.Length-1) + "Scope";
			else if (scopeType.IsInterface)
				Debug.LogWarningFormat("The Scope interface type {0} should be renamed to I{0} in order to distinguish interfaces from concrete classes.", scopeType.Name);

			return name;
		}
	}
	
}