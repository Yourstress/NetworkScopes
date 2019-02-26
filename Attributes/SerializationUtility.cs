using System;
using System.Linq;
using System.Reflection;
using ProtoBuf;

namespace NetworkScopes
{
	public static class SerializationUtility
	{
		public static string GetReadMethodName(Type type)
		{
			// check if it's an enum type with an underlying field
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			// see if IncomingNetworkPacket can read it
			MethodInfo readMethod = typeof(IncomingNetworkPacket)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(m => m.ReturnType == type && m.GetParameters().Length == 0);

			if (readMethod != null)
				return readMethod.Name;

			// if not found, read it with protobuf
			return $"ReadObject<{type.GetTypeName()}>";
		}

		public static string GetWriteMethodName(Type type)
		{
			// check if it's an enum type with an underlying field
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			// see if OutgoingNetworkPacket can write it
			MethodInfo writeMethod = typeof(OutgoingNetworkPacket)
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(m =>
				{
					ParameterInfo[] parameters = m.GetParameters();
					return parameters.Length == 1 &&
					       parameters[0].ParameterType == type &&
					       m.ReturnType == typeof(void);
				});

			if (writeMethod != null)
				return writeMethod.Name;

			// if not found, write it with protobuf
			return $"WriteObject<{type.GetTypeName()}>";
		}

		public static string MakeWriteStatement(string varName, string writerVarName, Type type)
		{
			string writeMethodName = GetWriteMethodName(type);

			string cast = type.IsEnum ? $"({type.GetEnumUnderlyingType().GetTypeName()})" : "";

			return $"{writerVarName}.{writeMethodName}({cast}{varName});";
		}

		public static string MakeReadCall(string readerVarName, Type type)
		{
			string readMethodName = GetReadMethodName(type);

			return $"{readerVarName}.{readMethodName}()";
		}

		public static string MakeReadAndAssignStatement(string assignVarName, string readVarName, Type type)
		{
			string typeName = type.GetTypeName();
			string cast = type.IsEnum ? $"({typeName})" : "";

			return $"{typeName} {assignVarName} = {cast}{MakeReadCall(readVarName, type)};";
		}

		public static bool CanSerializeAtRuntime(this Type type)
		{
			return CanSerializeUsingISerializable(type) || CanSerializeUsingProtobuf(type);
		}

		public static bool CanSerializeUsingISerializable(this Type type)
		{
			return type.GetInterfaces().Contains(typeof(ISerializable));
		}

		public static bool CanSerializeUsingProtobuf(this Type type)
		{
			return TypeUtility.IsNativeType(type) || type.IsEnum || type.GetCustomAttribute(typeof(ProtoContractAttribute)) != null;
		}
	}
}