using Lidgren.Network;

namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using CodeGeneration;

	public static partial class NetworkScopeUtility
	{
		public static readonly Type writerType = typeof(NetOutgoingMessage);
		public static readonly Type readerType = typeof(NetIncomingMessage);
		public static readonly ConstructorInfo writerCtor = ReflectionUtility.GetParameterlessConstructor(writerType);
		public static readonly ClassDefinition SerializerClass = new ClassDefinition("Serializers");
		public static readonly List<ClassDefinition> customSerializers = new List<ClassDefinition>();

		public static List<NetworkScopeAuthenticatorType> GetAuthenticatorTypes()
		{
			List<NetworkScopeAuthenticatorType> netScopeTypes = new List<NetworkScopeAuthenticatorType>(2);

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type t in assembly.GetTypes())
				{
					// find all MasterServers and MasterClients
					if (t.BaseType != null)
					{
						if (t.BaseType == typeof(MasterClient))
							netScopeTypes.Add(new NetworkScopeAuthenticatorType(t, false));
						if (t.BaseType.IsGenericType &&
						    t.BaseType.GetGenericTypeDefinition() == typeof(MasterServer<>))
							netScopeTypes.Add(new NetworkScopeAuthenticatorType(t, true));
					}
				}
			}

			return netScopeTypes;
		}

		public static List<NetworkScopeProcessor> GetNetworkScopeTypes()
		{
			List<NetworkScopeProcessor> netScopeTypes = new List<NetworkScopeProcessor>(20);

			Type injectorAttributeType = typeof(ScopeAttribute);
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in assembly.GetTypes())
				{
					object[] attrs = type.GetCustomAttributes(false);

					if (attrs.Length == 0)
						continue;

					for (int x = 0; x < attrs.Length; x++)
					{
						if (attrs[x].GetType() == injectorAttributeType)
						{
							ScopeAttribute attr = (ScopeAttribute)attrs[x];
							netScopeTypes.Add(new NetworkScopeProcessor(type, attr));
							break;
						}
					}
				}
			}

			return netScopeTypes;
		}


		public static MethodDefinition GetPartialScopeInitializer(ClassDefinition classDef)
		{
			MethodDefinition partialInitializeMethod = classDef.methods.FirstOrDefault(m => m.Name == "InitializePartialScope" && m.modifier == MethodModifier.Override);

			if (partialInitializeMethod == null)
			{
				partialInitializeMethod = new MethodDefinition("InitializePartialScope");
				partialInitializeMethod.modifier = MethodModifier.Override;
				partialInitializeMethod.accessModifier = MethodAccessModifier.Protected;

				classDef.methods.Insert(0, partialInitializeMethod);
			}

			return partialInitializeMethod;
		}

		public static MethodInfo GetNetworkWriterSerializer(Type type)
		{
			MethodInfo[] methods = writerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

			// find a method called Write that takes 1 parameter matching the type
			for (int x = 0; x < methods.Length; x++)
			{
				if (methods[x].Name == "Write")
				{
					ParameterInfo[] methodParams = methods[x].GetParameters();

					if (methodParams.Length == 1 && methodParams[0].ParameterType == type)
						return methods[x];
				}
			}

			return null;
		}



		public static MethodInfo GetNetworkReaderDeserializer(Type type)
		{
			MethodInfo[] methods = readerType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

			// find a method called ReadXXX that takes 0 parameters matching the type
			for (int x = 0; x < methods.Length; x++)
			{
				if (methods[x].Name.StartsWith("Read") && methods[x].GetParameters().Length == 0)
				{
					if (methods[x].ReturnType == type)
						return methods[x];
				}
			}

			return null;
		}

		internal static string MakeNetObjRecvMethodName(string fieldName)
		{
			return "Receive_" + char.ToUpper(fieldName[0]) + fieldName.Substring(1, fieldName.Length - 1); 
		}
	}
}