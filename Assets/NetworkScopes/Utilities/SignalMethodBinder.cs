
namespace NetworkScopes
{
	using System;
	using System.Linq;
	using System.Reflection;
	using System.Collections.Generic;

	public class SignalInvocation
	{
		public MethodInfo method;
		public object[] parameters;

		public void Invoke(object rootObject)
		{
			Invoke(rootObject, method, parameters);
		}

		public static void Invoke(object rootObject, MethodInfo method, object[] parameters)
		{
			try
			{
				method.Invoke(rootObject, parameters);
			}
			catch (Exception e)
			{
				if (method != null)
					Debug.LogError($"Failed to call method {method.DeclaringType.Name}.{method.Name}");
				else
					Debug.LogError($"Failed to call unbound method in {rootObject.GetType().Name}");

				Debug.LogException(e.InnerException ?? e);
			}
		}
	}

	public static class SignalMethodBinder
	{
		private static Dictionary<Type, Deserializer> cachedDeserializers = new Dictionary<Type, Deserializer>(4);
		
		class Deserializer : Dictionary<int, MethodInfo>
		{
			public Deserializer(Type scopeType)
			{
				List<MethodInfo> methods = scopeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy).ToList();

				foreach (MethodInfo method in methods)
				{
					// skip public non-receive functions
					if (method.IsPublic)
						continue;

					string name;
					int hash;
					if (method.Name.StartsWith("ReceiveSignal_"))
					{
						name = method.Name.Remove(0, 14);
						hash = name.GetConsistentHashCode();
					}
					else if (method.Name.StartsWith("ReceivePromise_"))
					{
						name = method.Name.Remove(0, 15);
						hash = ("#" + name).GetConsistentHashCode();
					}
					else
						continue;

					Add(hash, method);
				}
			}
		}

		public static void BindScope(IServerScope serverScope)
		{
			BindScopeInternal(serverScope.GetType(), typeof(ServerScope<,>));
		}

		public static void BindScope(IClientScope clientScope)
		{
			BindScopeInternal(clientScope.GetType(), typeof(ClientScope<>));
		}

		private static void BindScopeInternal(Type scopeType, Type requiredBaseType)
		{
			Type scopeParentType = scopeType;
			while (scopeParentType.BaseType != null && (!scopeParentType.BaseType.IsGenericType || scopeParentType.BaseType.GetGenericTypeDefinition() != requiredBaseType))
				scopeParentType = scopeParentType.BaseType;

			if (!cachedDeserializers.ContainsKey(scopeType))
			{
				cachedDeserializers[scopeType] = new Deserializer(scopeParentType);
			}
		}

		public static SignalInvocation GetMessageInvocation(Type scopeType, ISignalReader reader)
		{
			int signalType = reader.ReadInt32();

			// create a new invocation object and assign its method and reader parameter
			SignalInvocation invocation = new SignalInvocation();
			invocation.method = GetMethod(scopeType, signalType);
			invocation.parameters = new object[] { reader };

			return invocation;
		}

		public static void Invoke(object rootObject, Type scopeType, ISignalReader reader)
		{
			int signalType = reader.ReadInt32();
			MethodInfo method = GetMethod(scopeType, signalType);

			SignalInvocation.Invoke(rootObject, method, new object[] { reader });
		}

		private static MethodInfo GetMethod(Type scopeType, int signalType)
		{
			Deserializer deserializer = cachedDeserializers[scopeType];

			if (!deserializer.TryGetValue(signalType, out MethodInfo method))
			{
				Debug.LogError($"Could not find method with signal type {signalType} in {scopeType.Name}");
			}

			return method;
		}
	}

}