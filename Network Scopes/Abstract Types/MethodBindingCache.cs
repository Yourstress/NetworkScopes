
namespace NetworkScopesV2
{
	using System;
	using System.Reflection;
	using System.Collections.Generic;
	using System.Linq;

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
					ScopeUtils.LogWarning("Failed to call method {0}.{1}", method.DeclaringType.Name, method.Name);
				else
					ScopeUtils.LogWarning("Failed to call unbound method in {0}", rootObject.GetType().Name);

				ScopeUtils.LogException(e);
			}
		}
	}

	public static class MethodBindingCache
	{
		class Deserializer : Dictionary<int, MethodInfo>
		{
			public Deserializer(Type scopeType)
			{
				List<MethodInfo> methods = scopeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).ToList();

				List<FieldInfo> eventFields = scopeType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(f =>
				{
					return NetworkEventUtility.IsEventType(f.FieldType.Name);
				}).ToList();

				foreach (FieldInfo field in eventFields)
				{
					MethodInfo recvMethod = methods.Find(m => m.Name == string.Format("Receive_{0}", field.Name));

					// if no method was found, don't create the delegate
					if (recvMethod == null)
					{
						ScopeUtils.LogError("Network Scope: Skipping method {0} in {1} because no \"Receive_{0}\" function was found. The type is probably missing the [ClientSignalSync(typeof(SERVER_TYPE))] or [ServerSignalSync(typeof(CLIENT_TYPE))] attribute", field.Name, scopeType.Name);
						continue;
					}

					Add(field.Name.GetConsistentHashCode(), recvMethod);
				}

				foreach (MethodInfo method in methods)
				{
					// skip nonpublic or any send/receive methods
					if (!method.IsPublic || method.Name.StartsWith("Receive_") || method.Name.StartsWith("Send_"))
						continue;

					// find corresponding receive method
					MethodInfo recvMethod = methods.Find(m => m.Name == string.Format("Receive_{0}", method.Name));

					// if no method was found, then don't create the delegate
					if (recvMethod == null)
						continue;

					Add(method.Name.GetConsistentHashCode(), recvMethod);
				}
			}
		}

		private static Dictionary<Type, Deserializer> cachedDeserializers = new Dictionary<Type, Deserializer>(4);

		public static void BindScope(Type scopeType)
		{
			if (!cachedDeserializers.ContainsKey(scopeType))
			{
				cachedDeserializers[scopeType] = new Deserializer(scopeType);
			}
		}

		public static SignalInvocation GetMessageInvocation(Type scopeType, IMessageReader reader)
		{
			int signalType = reader.ReadInt32();

			// create a new invocation object and assign its method and reader parameter
			SignalInvocation invocation = new SignalInvocation();
			invocation.method = GetMethod(scopeType, signalType);
			invocation.parameters = new object[] { reader };

			return invocation;
		}

		public static void Invoke(object rootObject, Type scopeType, IMessageReader reader)
		{
			int signalType = reader.ReadInt32();

			MethodInfo method = GetMethod(scopeType, signalType);

			SignalInvocation.Invoke(rootObject, method, new object[] { reader });
		}

		private static MethodInfo GetMethod(Type scopeType, int signalType)
		{
			Deserializer deserializer = cachedDeserializers[scopeType];

			MethodInfo method;

			if (!deserializer.TryGetValue(signalType, out method))
			{
				ScopeUtils.LogError("Could not find method with signal type {0} in {1}", signalType, scopeType.Name);
			}


			return method;
		}
	}
	
}
