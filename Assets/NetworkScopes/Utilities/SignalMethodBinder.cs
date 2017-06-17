
namespace NetworkScopes
{
	using UnityEngine;
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
//			try
//			{
				method.Invoke(rootObject, parameters);
//			}
//			catch (Exception e)
//			{
//				if (method != null)
//					Debug.LogErrorFormat("Failed to call method {0}.{1}", method.DeclaringType.Name, method.Name);
//				else
//					Debug.LogErrorFormat("Failed to call unbound method in {0}", rootObject.GetType().Name);

//				Debug.LogException(e);
//			}
		}
	}

	public static class SignalMethodBinder
	{
		class Deserializer : Dictionary<int, MethodInfo>
		{
			public Deserializer(Type scopeType)
			{
				List<MethodInfo> methods = scopeType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).ToList();

				foreach (MethodInfo method in methods)
				{
					// skip public non-receive functions
					if (method.IsPublic || !method.Name.StartsWith("Receive_"))
					{
						continue;
					}
					
					string name = method.Name.Remove(0, 8);
					Add(name.GetHashCode(), method);
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

			MethodInfo method;

			if (!deserializer.TryGetValue(signalType, out method))
			{
				Debug.LogErrorFormat("Could not find method with signal type {0} in {1}", signalType, scopeType.Name);
			}


			return method;
		}
	}

}