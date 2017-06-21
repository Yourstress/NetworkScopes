using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace NetworkScopes.CodeGeneration
{
	public enum SerializationFailureReason
	{
		TypeNotSerializable,
	}

	public enum DeserializationOptions
	{
		AllocateVariable,
		DontAllocateVariable,
	}

	public class SerializationProvider
	{
		public HashSet<Type> serializableTypes { get; private set; }
		public Dictionary<Type, SerializationFailureReason> failedTypes { get; private set; }

		public SerializationProvider()
		{
			serializableTypes = new HashSet<Type>();
			failedTypes = new Dictionary<Type, SerializationFailureReason>();
		}

		public void AddSerializationCommands(MethodBody targetMethod, string variableName, Type variableType)
		{
			// if this is an array, generate a for-loop to serialize every item in it
			if (variableType.IsArray || (variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(List<>)))
			{
				Type elementType = variableType.IsArray ? variableType.GetElementType() : variableType.GetGenericArguments()[0];

				// if it's a serializable type, there's a one liner method to serialize it
				if (typeof(ISerializable).IsAssignableFrom(elementType) || serializableTypes.Contains(elementType))
				{
					if (variableType.IsArray)
						targetMethod.AddMethodCall("writer", string.Format("WriteObjectArray<{0}>", elementType.Name), variableName);
					else
					{
						// import the needed namespace for generic List type
						targetMethod.Import(typeof(List<>).Namespace);

						targetMethod.AddMethodCall("writer", string.Format("WriteObjectList<{0}>", elementType.Name), variableName);
					}
				}
				// otherwise, serialize it inline
				else
				{
					string lengthVarName = variableName + ".Length";

					// serialize array length
					AddSerializationCommands(targetMethod, lengthVarName, typeof(int));

					// nested-serialize everything in the array
					string loopVarName = variableName + "_x";
					targetMethod.BeginForIntLoop(loopVarName, "0", lengthVarName);
					AddSerializationCommands(targetMethod, string.Format("{0}[{1}]", variableName, loopVarName), elementType);
					targetMethod.EndForIntLoop();
				}
				return;
			}

			// find primitive type serializer method within ISignalWriter
			MethodInfo paramWriteMethod = SignalUtility.GetWriterMethod(variableType);

			// if it's found, immediately write the command to serialize it into the signal writer
			if (paramWriteMethod != null)
			{
				targetMethod.AddMethodCall("writer", paramWriteMethod.Name, variableName);
				return;
			}

			// see if the type is marked for network serialization
			NetworkSerializeAttribute serializeAttribute = variableType.GetCustomAttribute<NetworkSerializeAttribute>();

			// add it to list of types that need to be serialized
			if (serializeAttribute != null)
				serializableTypes.Add(variableType);

			// could not find a native writer? look for an implementor of ISerializable
			bool typeImplementsSerializable = typeof(ISerializable).IsAssignableFrom(variableType);

			// write serialization command(s) if it implements ISerializable OR if it's marked for code generation
			if (typeImplementsSerializable || serializeAttribute != null)
			{
				targetMethod.AddMethodCall(variableName, "Serialize", "writer");
				return;
			}

			// failed at this point
			failedTypes[variableType] = SerializationFailureReason.TypeNotSerializable;
		}

		public void AddDeserializationCommands(MethodBody targetMethod, string variableName, Type variableType, DeserializationOptions deserializationOptions = DeserializationOptions.AllocateVariable)
		{
			// if this is an array, generate a for-loop to deserialize it and every item in it
			if (variableType.IsArray || (variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(List<>)))
			{
				Type elementType = variableType.IsArray ? variableType.GetElementType() : variableType.GetGenericArguments()[0];

				// if it's a serializable type, there's a one liner method to serialize it
				if (typeof(ISerializable).IsAssignableFrom(elementType) || serializableTypes.Contains(elementType))
				{
					// CODE: T[] var = reader.ReadObjectArray<T>();
					if (variableType.IsArray)
						targetMethod.AddMethodCallWithAssignment(variableName, string.Format("{0}[]", elementType.Name), "reader",
							string.Format("ReadObjectArray<{0}>", elementType));
					// CODE: List<T> var = reader.ReadObjectList<T>();
					else
					{
						// import the needed namespace for generic List type
						targetMethod.Import(typeof(List<>).Namespace);

						targetMethod.AddMethodCallWithAssignment(variableName, string.Format("List<{0}>", elementType.Name), "reader", string.Format("ReadObjectList<{0}>", elementType));
					}
				}
				// otherwise, serialize it inline
				else
				{
					// nested-deserialize array
					string lengthVarName = variableName + "_length";

					// CODE: int varLength = reader.ReadInt32();
					AddDeserializationCommands(targetMethod, lengthVarName, typeof(int));

					// CODE: T[] var = new T[length];
					if (variableType.IsArray)
					{
						targetMethod.AddAssignmentInstruction(variableType, variableName, string.Format("new {0}[{1}]", elementType.Name, lengthVarName));
					}
					// CODE: List<T> var = new List<T>[length];
					else
					{
						// import the needed namespace for generic List type
						targetMethod.Import(typeof(List<>).Namespace);

						TypeDefinition genericVariableType = TypeDefinition.MakeGenericType(variableType, elementType);

						targetMethod.AddAssignmentInstruction(genericVariableType, variableName, string.Format("new List<{0}>({1})", elementType.Name, lengthVarName));
					}

					// CODE: for loop and nested serialization
					string loopVarName = variableName + "_x";
					targetMethod.BeginForIntLoop(loopVarName, "0", lengthVarName);
					AddDeserializationCommands(targetMethod, string.Format("{0}[{1}]", variableName, loopVarName), elementType, DeserializationOptions.DontAllocateVariable);
					targetMethod.EndForIntLoop();
				}

				return;
			}

			// find primitive type deserializer method within ISignalReader
			MethodInfo paramReadMethod = SignalUtility.GetReaderMethod(variableType);

			// if it's found, immediately write the command to deserialize it from the signal reader
			if (paramReadMethod != null)
			{
				if (deserializationOptions == DeserializationOptions.AllocateVariable)
					targetMethod.AddMethodCallWithAssignment(variableName, variableType.GetReadableName(), "reader", paramReadMethod.Name);
				else if (deserializationOptions == DeserializationOptions.DontAllocateVariable)
					targetMethod.AddMethodCallWithAssignment(variableName, "reader", paramReadMethod.Name);
				else
					throw new Exception("Undefined DeserializaOption");
				return;
			}

			// see if the type is marked for network serialization
			NetworkSerializeAttribute serializeAttribute = variableType.GetCustomAttribute<NetworkSerializeAttribute>();

			// add it to list of types that need to be serialized
			if (serializeAttribute != null)
				serializableTypes.Add(variableType);

			// could not find a native writer? look for an implementor of ISerializable
			bool typeImplementsSerializable = typeof(ISerializable).IsAssignableFrom(variableType);

 			// write serialization command(s) if it implements ISerializable OR if it's marked for code generation
			if (typeImplementsSerializable || serializeAttribute != null)
			{
				// create the object to serialize the data into
				if (deserializationOptions == DeserializationOptions.AllocateVariable)
					targetMethod.AddAssignmentInstruction(variableType, variableName, string.Format("new {0}()", variableType.Name));
				else if (deserializationOptions == DeserializationOptions.DontAllocateVariable)
					targetMethod.AddAssignmentInstruction(variableName, string.Format("new {0}()", variableType));
				else
					throw new Exception("Undefined DeserializationOption.");

				// call the deserialize method and read the value into the newly created variable
				targetMethod.AddMethodCall(variableName, "Deserialize", "reader");

				// add to list of type serializers to be generated
				serializableTypes.Add(variableType);
				return;
			}

			// failed at this point
			failedTypes[variableType] = SerializationFailureReason.TypeNotSerializable;
		}

		private string GetTypeSerializerClassPath(Type serializableType)
		{
			// name will always be saved with a "_Serialization" suffix
			string fileName = string.Format("{0}_Serialization", serializableType.Name);

			string[] assetGuids = AssetDatabase.FindAssets(string.Format("t:MonoScript {0}", fileName));
			string assetPath;

			if (assetGuids.Length == 0)
				assetPath = string.Format("Assets/{0}.cs", fileName);
			else
				assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[0]);

			return assetPath;
		}

		public void GenerateTypeSerializers()
		{
			foreach (Type serializableType in serializableTypes)
			{
				ClassDefinition typeSerializerDef = CreateTypeSerializerClass(serializableType);

				string path = GetTypeSerializerClassPath(serializableType);
				File.WriteAllText(path, typeSerializerDef.ToScriptWriter().ToString());
			}
		}

		public ClassDefinition CreateTypeSerializerClass(Type serializableType)
		{
			ClassDefinition typeSerializer = new ClassDefinition(serializableType.Name);
			typeSerializer.type.Namespace = serializableType.Namespace;
			typeSerializer.isPartial = true;

			typeSerializer.interfaces.Add(typeof(ISerializable));

			MemberInfo[] membersToSerialize = GetSerializationMembers(serializableType);

			// create WRITER method
			MethodDefinition writerMethod = new MethodDefinition("Serialize");
			writerMethod.Parameters.Add(new ParameterDefinition("writer", typeof(ISignalWriter)));

			foreach (MemberInfo member in membersToSerialize)
			{
				Type type = ((member is FieldInfo) ? ((FieldInfo) member).FieldType : ((PropertyInfo)member).PropertyType);
				AddSerializationCommands(writerMethod.Body, member.Name, type);
			}

			// create READER method
			MethodDefinition readerMethod = new MethodDefinition("Deserialize");
			readerMethod.Parameters.Add(new ParameterDefinition("reader", typeof(ISignalReader)));

			foreach (MemberInfo member in membersToSerialize)
			{
				Type type = ((member is FieldInfo) ? ((FieldInfo) member).FieldType : ((PropertyInfo)member).PropertyType);
				AddDeserializationCommands(readerMethod.Body, member.Name, type, DeserializationOptions.DontAllocateVariable);
			}

			typeSerializer.methods.Add(writerMethod);
			typeSerializer.methods.Add(readerMethod);

			return typeSerializer;
		}

		private static MemberInfo[] GetSerializationMembers(Type serializableTypes)
		{
			List<MemberInfo> members = new List<MemberInfo>();

			members.AddRange(serializableTypes.GetFields());
			members.AddRange(serializableTypes.GetProperties());

			return members.ToArray();
		}

		public static TypeDefinition GetPromiseType(Type type)
		{
			Type mainPromiseType = (type.Namespace == "System") ? typeof(ValuePromise<>) : typeof(ObjectPromise<>);

			return TypeDefinition.MakeGenericType(mainPromiseType, type.GetReadableName());
		}
	}
}