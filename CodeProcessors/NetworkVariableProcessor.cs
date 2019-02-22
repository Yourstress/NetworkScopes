
using UnityEngine;

namespace NetworkScopes.CodeProcessors
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using CodeGeneration;

	public static class NetworkVariableProcessor
	{
		static readonly Type[] NetworkVariableClassTypes = { typeof(NetworkObject<>), typeof(NetworkValue<>), typeof(NetworkList<>) };
		static readonly Type[] NetworkVariableInterfaceTypes = { typeof(INetworkObject<>), typeof(INetworkValue<>), typeof(INetworkList<>) };

		public enum VariableType
		{
			Value,
			Object,
			List,
			Auto,
		}

		public static void GenerateNetworkVariableCode(ClassDefinition classDef, FieldInfo[] netObjFields, bool areLocalFields)
		{
			MethodDefinition partialInitializeMethod = NetworkScopeUtility.GetPartialScopeInitializer(classDef);

			foreach (FieldInfo netObjField in netObjFields)
			{
				Type netVariableType = netObjField.FieldType.GetGenericArguments()[0];
				IVariableSerialization netVariableSerialization = GetNetworkVariableFieldSerialization(netObjField.FieldType, VariableType.Auto);

				if (netVariableSerialization == null)
				{
					Debug.LogErrorFormat($"Failed to serialize NetworkObject<{netVariableType.Name}> in {classDef.Name} because the type {netVariableType.Name} is not serializable.");
					return;
				}

				classDef.imports.Add(netVariableType.Namespace);

				// initialize each NetworkObject
				string fieldName = netObjField.Name;
				int netObjId = fieldName.GetConsistentHashCode();
				string refPrefix = areLocalFields ? "ref " : "";

				string serializerTypeName = netVariableSerialization.isValueType ?
																	netVariableSerialization.serializeMethod.DeclaringType.Name :
																	netVariableType.Name;

				partialInitializeMethod.instructions.AddMethodCall("Register" + netVariableSerialization.NetworkVariableType, new string[]
				{
					refPrefix + fieldName,
					netObjId.ToString(),
					$"{serializerTypeName}.{netVariableSerialization.serializeMethod.Name}",
					$"{serializerTypeName}.{netVariableSerialization.deserializeMethod.Name}"
				});

				if (!areLocalFields)
				{
					// and create a receiver method if this is a remote scope
					string recvMethodName = NetworkScopeUtility.MakeNetObjRecvMethodName(netObjField.Name);
					MethodDefinition recvMethod = new MethodDefinition(recvMethodName);
					recvMethod.parameters.Add(new ParameterDefinition("reader", NetworkScopeUtility.readerType));
					recvMethod.instructions.AddMethodCall(fieldName, "Read", "reader");

					classDef.methods.Add(recvMethod);

					// as well as a local field that's accessible on the Scope object
					FieldDefinition remoteNetObjField = new FieldDefinition(fieldName, $"I{netVariableSerialization.NetworkVariableType}<{netVariableType.Name}>", true);
					remoteNetObjField.assignment = $"{netVariableSerialization.NetworkVariableType}<{netVariableType.Name}>.FromObjectID({netObjId})";
					classDef.fields.Add(remoteNetObjField);
				}
			}
		}

		#region Helpers
		public static IVariableSerialization GetVariableSerialization(Type type, bool isList, VariableType variableType)
		{
			// get serialization generator whether it's a value or object type
			IVariableSerialization vs = null;

			if (variableType == VariableType.Value || variableType == VariableType.Auto)
				vs = ValueSerialization.FromType(type);

			if (vs == null && (variableType == VariableType.Object || variableType == VariableType.List || variableType == VariableType.Auto))
				vs = ObjectSerialization.FromType(type, isList);

			return vs;
		}

		public static IVariableSerialization GetNetworkVariableFieldSerialization(Type fieldType, VariableType variableType)
		{
			Type type = fieldType.GetGenericArguments()[0];
			bool isList = fieldType.GetGenericTypeDefinition() == typeof(NetworkList<>);
			return GetVariableSerialization(type, isList, variableType);
		}

		public static IEnumerable<FieldInfo> GetNetworkVariableFields(Type classType, bool concreteImplementationOnly)
		{
			return classType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(f => IsNetworkVariableType(f.FieldType, concreteImplementationOnly));
		}

		public static bool IsNetworkVariableType(Type fieldType, bool concreteImplementationOnly)
		{
			Func<Type,bool> isValidVariableType;
			if (fieldType.IsGenericType)
				isValidVariableType = t => t.IsGenericType && t == fieldType.GetGenericTypeDefinition();
			else
				isValidVariableType = t => t == fieldType;

			// return true if concrete type match was found
			if (NetworkVariableClassTypes.Any(isValidVariableType))
				return true;

			// return true if interface match was found ONLY specified
			if (!concreteImplementationOnly)
				return NetworkVariableInterfaceTypes.Any(isValidVariableType);

			return false;
		}
		#endregion
	}
}
