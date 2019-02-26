
namespace NetworkScopes
{
	using System;
	using System.Linq;
	using System.Reflection;
	using System.Collections.Generic;
	using NetworkScopes.CodeProcessors;

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
					NetworkDebug.LogErrorFormat("Failed to call method {0}.{1}", method.DeclaringType.Name, method.Name);
				else
					NetworkDebug.LogErrorFormat("Failed to call unbound method in {0}", rootObject.GetType().Name);

				NetworkDebug.LogException(e);
			}
		}
	}

	public static class MethodBindingCache
	{
		class Deserializer : Dictionary<int, MethodInfo>
		{
			public Deserializer(Type scopeType)
			{
				MethodInfo[] methods = scopeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).ToArray();

				foreach (MethodInfo method in methods)
				{
					if (method.Name.StartsWith("Response_") && method.IsPublic)
					{
						Add(method.Name.GetConsistentHashCode(), method);
						continue;
					}
					// skip nonpublic or any send/receive methods
					if (!method.IsPublic || method.Name.StartsWith("Receive_") || method.Name.StartsWith("Send_"))
						continue;

					// find corresponding receive method
					MethodInfo recvMethod = methods.FirstOrDefault(m => m.Name == string.Format("Receive_{0}", method.Name));

					// if no method was found, then don't create the delegate
					if (recvMethod == null)
						continue;

					if (recvMethod.ReturnType != typeof(void))
						NetworkDebug.Log("Found " + recvMethod.Name);

					Add(method.Name.GetConsistentHashCode(), recvMethod);
				}

				RegisterEvents(scopeType, methods);

				RegisterNetworkObjects(scopeType, methods);
			}

			void RegisterNetworkObjects(Type scopeType, MethodInfo[] methods)
			{
				// TODO: register network objects
//				FieldInfo[] netObjFields = NetworkVariableProcessor.GetNetworkVariableFields(scopeType, false).ToArray();
//
//				foreach (FieldInfo field in netObjFields)
//				{
//					// find receiving method for the given NetworkObject typed field
//					MethodInfo recvMethod = methods.FirstOrDefault(m => m.IsPublic && m.Name == NetworkScopeUtility.MakeNetObjRecvMethodName(field.Name));
//
//					// if no method was found, then don't create the delegate
//					if (recvMethod == null)
//						continue;
//
//					Add(field.Name.GetConsistentHashCode(), recvMethod);
//				}
			}

			void RegisterEvents(Type scopeType, MethodInfo[] methods)
			{
				// TODO: register network events
//				FieldInfo[] eventFields = scopeType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(f =>
//				{
//					return NetworkEventUtility.IsEventType(f.FieldType.Name);
//				}).ToArray();
//
//				foreach (FieldInfo field in eventFields)
//				{
//					MethodInfo recvMethod = methods.FirstOrDefault(m => m.Name == string.Format("Receive_{0}", field.Name));
//
//					// if no method was found, don't create the delegate
//					if (recvMethod == null)
//					{
//						NetworkDebug.LogFormat("Network Scope: Skipping method {0} in {1} because no \"Receive_{0}\" function was found. The type is probably missing the [ClientSignalSync(typeof(SERVER_TYPE))] or [ServerSignalSync(typeof(CLIENT_TYPE))] attribute", field.Name, scopeType.Name);
//						continue;
//					}
//
//					Add(field.Name.GetConsistentHashCode(), recvMethod);
//				}
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

		public static SignalInvocation GetMessageInvocation(Type scopeType, IncomingNetworkPacket packet)
		{
			int signalType = packet.ReadInt();

			// create a new invocation object and assign its method and reader parameter
			SignalInvocation invocation = new SignalInvocation();
			invocation.method = GetMethod(scopeType, signalType);
			invocation.parameters = new object[] { packet };

			return invocation;
		}

		public static void Invoke(object rootObject, Type scopeType, IncomingNetworkPacket packet)
		{
			int signalType = packet.ReadInt();

			MethodInfo method = GetMethod(scopeType, signalType);

			SignalInvocation.Invoke(rootObject, method, new object[] { packet });
		}

		private static MethodInfo GetMethod(Type scopeType, int signalType)
		{
			Deserializer deserializer = cachedDeserializers[scopeType];

			MethodInfo method;

			if (!deserializer.TryGetValue(signalType, out method))
			{
				NetworkDebug.LogErrorFormat("Could not find method with signal type {0} in {1}", signalType, scopeType.Name);
			}


			return method;
		}
	}
}
