using UnityEngine;
using System.Reflection;


namespace NetworkScopes.CodeProcessing
{
	using System;

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

		public static ScopeDefinition NewAuthenticatorScope(Type scopeType, Type interfaceType)
		{
			ScopeDefinition authScope = new ScopeDefinition(scopeType);
			authScope.SetBaseClass(interfaceType);
			authScope.IsInterface = true;
			authScope.IsAbstract = false;
			return authScope;
		}

		public Type scopeType { get; private set; }

		private ScopeDefinition(Type scopeType)
		{
			Name = MakeScopeName(scopeType);

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

			if (!name.StartsWith("I") && scopeType.IsInterface)
			{
				Debug.LogWarningFormat("The Scope interface type {0} should be renamed to I{0} in order to distinguish interfaces from concrete classes.", scopeType.Name);
			}
			else
			{
				// trim the 'I' from the class name
				name = name.Substring(1, name.Length-1) + "Scope";
			}

			return name;
		}
	}
	
}