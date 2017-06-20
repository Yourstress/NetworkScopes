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
		Default,
		DontAllocateVariables,
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
			bool implementsSerializable = typeof(ISerializable).IsAssignableFrom(variableType);

			// write serialization command(s) if it implements ISerializable OR if it's marked for code generation
			if (implementsSerializable || serializeAttribute != null)
			{
				targetMethod.AddMethodCall(variableName, "Serialize", "writer");
				return;
			}

			// failed at this point
			failedTypes[variableType] = SerializationFailureReason.TypeNotSerializable;
		}

		public void AddDeserializationCommands(MethodBody targetMethod, string variableName, Type variableType, DeserializationOptions deserializationOptions = DeserializationOptions.Default)
		{
			// find primitive type deserializer method within ISignalReader
			MethodInfo paramReadMethod = SignalUtility.GetReaderMethod(variableType);

			// if it's found, immediately write the command to deserialize it from the signal reader
			if (paramReadMethod != null)
			{
				if (deserializationOptions == DeserializationOptions.DontAllocateVariables)
					targetMethod.AddMethodCallWithAssignment(variableName, "reader", paramReadMethod.Name);
				else
					targetMethod.AddMethodCallWithAssignment(variableName, variableType.GetReadableName(), "reader", paramReadMethod.Name);
				return;
			}

			// see if the type is marked for network serialization
			NetworkSerializeAttribute serializeAttribute = variableType.GetCustomAttribute<NetworkSerializeAttribute>();

			// add it to list of types that need to be serialized
			if (serializeAttribute != null)
				serializableTypes.Add(variableType);

			// could not find a native writer? look for an implementor of ISerializable
			bool implementsSerializable = typeof(ISerializable).IsAssignableFrom(variableType);

 			// write serialization command(s) if it implements ISerializable OR if it's marked for code generation
			if (implementsSerializable || serializeAttribute != null)
			{
				// create the object to serialize the data into
				targetMethod.AddAssignmentInstruction(variableType, variableName, string.Format("new {0}();", variableType.Name));

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
				AddDeserializationCommands(readerMethod.Body, member.Name, type, DeserializationOptions.DontAllocateVariables);
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