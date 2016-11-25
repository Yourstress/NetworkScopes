
namespace NetworkScopes.CodeProcessing
{
	using UnityEngine;
	using System.Reflection;
	using System;
	using System.Collections.Generic;

	public class ScopeDefinition : ClassDefinition
	{
		public bool HasRuntimeConcreteType { get { return scopeType != null && scopeType.Assembly.GetType(FullName) != null; } }
		public Type scopeType { get; private set; }

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

		private ScopeDefinition(string scopeName, string scopeNamespace) : base(scopeName, scopeNamespace)
		{
		}

		private ScopeDefinition(Type scopeType)
		{
			Name = MakeScopeName(scopeType);
			Namespace = scopeType.Namespace;

			IsAbstract = true;
			AddMethods(scopeType, true, false);

			this.scopeType = scopeType;
		}

		public ScopeDefinition CreateConcreteClassDefinition()
		{
			// make sure the file doesn't already exist
			string concreteClassName = Name.Replace("Scope", string.Empty);

			// create scope concrete class if it doesn't exist
			ScopeDefinition concreteScopeDef = new ScopeDefinition(concreteClassName, scopeType.Namespace);
			concreteScopeDef.BaseClass = Name;
			concreteScopeDef.AddMethods(scopeType, false, true);
			return concreteScopeDef;
		}

		protected override MethodDefinition AddMethod (MethodInfo method, bool isAbstract, bool isOverride)
		{
			MethodDefinition methodDef = base.AddMethod (method, isAbstract, isOverride);

			ParameterInfo[] parameters = method.GetParameters();
			List<Type> promiseTypes = null;
			for (int x = 0; x < parameters.Length; x++)
			{
				if (parameters[x].IsOut)
				{
					if (promiseTypes == null)
						promiseTypes = new List<Type>(parameters.Length);
					
					promiseTypes.Add(parameters[x].ParameterType);
				}
			}

			if (promiseTypes != null)
			{
				Type netPromiseType = typeof(NetworkPromise<>);
				string netPromiseName = CleanGenericTypeName(netPromiseType.Name);
				netPromiseName = AddGenericTypeParameters(netPromiseName, promiseTypes.ToArray()).Replace("&", string.Empty);

				Type matchingPromiseType = netPromiseType.Assembly.GetType(netPromiseType.FullName.Replace("`1","`"+promiseTypes.Count));

				methodDef.ReturnType = netPromiseName;

				if (matchingPromiseType == null)
					throw new Exception(string.Format("NetworkPromise does not have an implementation with {0} parameters for the method <color=white>{1}</color>.", promiseTypes.Count, method.Name));
			}

			return methodDef;
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