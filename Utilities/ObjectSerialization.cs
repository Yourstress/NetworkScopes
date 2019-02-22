using UnityEngine;

namespace NetworkScopes
{
	using System;
	using System.Reflection;

	public enum NetworkVariableType
	{
		NetworkValue,
		NetworkList,
		NetworkObject,
	}

	public interface IVariableSerialization
	{
		bool isValueType { get; }
		NetworkVariableType NetworkVariableType { get; }
		Type VariableType { get; }
		MethodInfo serializeMethod { get; }
		MethodInfo deserializeMethod { get; }
	}

	public static class VariableSerializationExtensions
	{
		public static string CreateNetworkSerializationMethodSignature(this IVariableSerialization serialization)
		{
			if (serialization.isValueType)
				return $"{serialization.serializeMethod.DeclaringType.Name}.{serialization.serializeMethod.Name}";

			string s = $"{TypeUtility.GetTypeName(serialization.VariableType)}.{serialization.serializeMethod.Name}";

			if (serialization.VariableType.DeclaringType != null)
				s = $"{TypeUtility.GetTypeName(serialization.VariableType.DeclaringType)}.{s}";
			return s;
		}

		public static string CreateNetworkDeserializationMethodSignature(this IVariableSerialization serialization)
		{
			if (serialization.isValueType)
				return $"{serialization.deserializeMethod.DeclaringType.Name}.{serialization.deserializeMethod.Name}";

			string s = $"{TypeUtility.GetTypeName(serialization.VariableType)}.{serialization.deserializeMethod.Name}";

			if (serialization.VariableType.DeclaringType != null)
				s = $"{TypeUtility.GetTypeName(serialization.VariableType.DeclaringType)}.{s}";
			return s;
		}

		public static string CreateNetworkSerializationCall(this IVariableSerialization serialization, string variableName, string msgVarName)
		{
			if (serialization.isValueType)
				return $"{serialization.serializeMethod.DeclaringType.Name}.{serialization.serializeMethod.Name}({variableName}, {msgVarName})";

			return $"{serialization.VariableType.Name}.{serialization.serializeMethod.Name}({variableName}, {msgVarName})";
		}
		public static string CreateNetworkDeserializeCall(this IVariableSerialization serialization, string msgVarName)
		{
			if (serialization.isValueType)
				return $"{serialization.deserializeMethod.DeclaringType.Name}.{serialization.deserializeMethod.Name}({msgVarName})";

			return $"{serialization.VariableType.Name}.{serialization.deserializeMethod.Name}({msgVarName})"; 
		}
	}

	public class ObjectSerialization : IVariableSerialization
	{
		public bool isValueType => false;
		public NetworkVariableType NetworkVariableType { get; private set; }
		public Type VariableType { get; private set; }
		public MethodInfo serializeMethod { get; private set; }
		public MethodInfo deserializeMethod { get; private set; }

		public static ObjectSerialization FromType(Type type, bool isList)
		{
			// check if class has auto-generated serializer
			Type generatedSerializerType = Type.GetType("Serializers");

			if (generatedSerializerType != null)
			{
				Type generatedSerializerClass = generatedSerializerType.GetNestedType($"{type.Name}Serializer");

				if (generatedSerializerClass != null)
					type = generatedSerializerClass;
			}

			MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

			MethodInfo serMethod = null;
			MethodInfo deserMethod = null;

			// find methods as long as one is null
			for (int x = 0; x < methods.Length && (serMethod == null || deserMethod == null); x++)
			{
				if (serMethod == null && methods[x].Name == "NetworkSerialize")
					serMethod = methods[x];
				if (deserMethod == null && methods[x].Name == "NetworkDeserialize")
					deserMethod = methods[x];
			}

			if (serMethod != null && deserMethod != null)
			{
				return new ObjectSerialization
				{
					serializeMethod = serMethod,
					deserializeMethod = deserMethod,
					VariableType = type,
					NetworkVariableType = isList ? NetworkVariableType.NetworkList : NetworkVariableType.NetworkObject
				};
			}

			Debug.Log("Could not serialize type " + type);

			return null;
		}
	}

	public class ValueSerialization : IVariableSerialization
	{
		public bool isValueType => true;
		public NetworkVariableType NetworkVariableType => NetworkVariableType.NetworkValue;
		public Type VariableType { get; private set; }
		public MethodInfo serializeMethod { get; private set; }
		public MethodInfo deserializeMethod { get; private set; }

		public static ValueSerialization FromType(Type type)
		{
			Type valueSerializerType = ValueSerializerTypes.GetSerializerClass(type);

			if (valueSerializerType == null)
				return null;

			// if the type exists, we'll assume the methods exist because we wrote them
			MethodInfo serMethod = valueSerializerType.GetMethod("NetworkSerialize");
			MethodInfo deserMethod = valueSerializerType.GetMethod("NetworkDeserialize");

			if (serMethod != null && deserMethod != null)
				return new ValueSerialization { serializeMethod = serMethod, deserializeMethod = deserMethod, VariableType = type };

			return null;
		}
	}
}