
using System;
using System.Reflection;
using CodeGeneration;
using NetworkScopes;
using UnityEngine.Networking;

public static class NetworkScopeUtility
{
	public static Type writerType { get; private set; }
	public static Type readerType { get; private set; }
	public static ConstructorInfo writerCtor { get; private set; }
	public static ClassDefinition SerializerClass { get; private set; }

	static NetworkScopeUtility()
	{
		writerType = typeof(NetworkWriter);
		readerType = typeof(NetworkReader);
		writerCtor = ReflectionUtility.GetParameterlessConstructor(writerType);
		SerializerClass = new ClassDefinition("NetworkSerializer");
	}

	public static MethodInfo GetCustomTypeSerializer(Type type)
	{
		MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

		for (int x = 0; x < methods.Length; x++)
		{
			if (methods[x].Name == "NetworkSerialize")
				return methods[x];
		}

		return null;
	}

	public static MethodInfo GetCustomTypeDeserializer(Type type)
	{
		MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

		for (int x = 0; x < methods.Length; x++)
		{
			if (methods[x].Name == "NetworkDeserialize")
				return methods[x];
		}

		return null;
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
}
