using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NetworkScopes.CodeGeneration
{
	public enum SerializationFailureReason
	{
		TypeNotSerializable,
	}

	public enum DeserializationOption
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
				string elemTypeName = elementType.GetReadableName();

				// if it's a serializable type, there's a one liner method to serialize it
				if (typeof(ISerializable).IsAssignableFrom(elementType) || serializableTypes.Contains(elementType))
				{
					// string methodName = variableType.IsArray ? $"WriteObjectArray<{elemTypeName}>" : $"WriteObjectList<{elemTypeName}>";

					if (variableType.IsArray)
						targetMethod.AddMethodCall("writer", $"WriteObjectArray<{elemTypeName}>", variableName);
					else
					{
						// import the needed namespace for generic List type
						targetMethod.Import(typeof(List<>).Namespace);

						targetMethod.AddMethodCall("writer", $"WriteObjectList<{elemTypeName}>", variableName);
					}
				}
				// otherwise, serialize it inline
				else
				{
					string lengthVarName = variableName + (variableType.IsArray ? ".Length" : ".Count");

					// serialize array length
					AddSerializationCommands(targetMethod, lengthVarName, typeof(int));

					// nested-serialize everything in the array
					string loopVarName = variableName + "_x";
					targetMethod.BeginForIntLoop(loopVarName, "0", lengthVarName);
					AddSerializationCommands(targetMethod, $"{variableName}[{loopVarName}]", elementType);
					targetMethod.EndLoop();
				}
				return;
			}
			if (variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				Type keyElementType = variableType.GetGenericArguments()[0];
				Type valElementType = variableType.GetGenericArguments()[1];

				targetMethod.Import(typeof(Dictionary<,>).Namespace);

				// if both key and value are serializable types, there's a one liner method to serialize it
				if (typeof(ISerializable).IsAssignableFrom(keyElementType) || serializableTypes.Contains(keyElementType) &&
				    typeof(ISerializable).IsAssignableFrom(valElementType) || serializableTypes.Contains(valElementType))
				{
					targetMethod.AddMethodCall("writer", $"WriteObjectDictionary<{keyElementType.GetReadableName()},{valElementType.GetReadableName()}>", variableName);
				}
				// otherwise, serialize it inline
				else
				{
					string lengthVarName = variableName + ".Count";

					// serialize dictionary length
					AddSerializationCommands(targetMethod, lengthVarName, typeof(int));

					// nested-serialize everything in the array
					TypeDefinition kvpType = TypeDefinition.MakeGenericType(typeof(KeyValuePair<,>), keyElementType, valElementType);
					targetMethod.BeginForEachLoop(kvpType, "kvp", variableName);
					AddSerializationCommands(targetMethod, "kvp.Key", keyElementType);
					AddSerializationCommands(targetMethod, "kvp.Value", valElementType);
					targetMethod.EndLoop();

				}
				return;
			}

			// find primitive type serializer method within ISignalWriter
			SignalWriterMethod paramWriteMethod = new SignalWriterMethod(variableType);

			// if it's found, immediately write the command to serialize it into the signal writer
			if (paramWriteMethod.IsAvailable)
			{
				paramWriteMethod.AddMethodCall(targetMethod, variableName);
				return;
			}

			// see if the type is marked for network serialization
			NetworkSerializeAttribute serializeAttribute = variableType.GetCustomAttribute<NetworkSerializeAttribute>();

			// add it to list of types that need to be serialized
			if (serializeAttribute != null)
				AddSerializedType(variableType);

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

		public void AddDeserializationCommands(MethodBody targetMethod, string variableName, Type variableType, DeserializationOption deserializationOption = DeserializationOption.AllocateVariable)
		{
			// if this is an array, generate a for-loop to deserialize it and every item in it
			if (variableType.IsArray || (variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(List<>)))
			{
				Type elementType = variableType.IsArray ? variableType.GetElementType() : variableType.GetGenericArguments()[0];
				string elemTypeName = elementType.GetReadableName();

				// if it's a serializable type, there's a one liner method to serialize it
				if (typeof(ISerializable).IsAssignableFrom(elementType) || serializableTypes.Contains(elementType))
				{
					string methodName = variableType.IsArray ? $"ReadObjectArray<{elemTypeName}>" : $"ReadObjectList<{elemTypeName}>";

					targetMethod.AddMethodCallWithAssignment(variableName, "reader", methodName, deserializationOption, elemTypeName);
					
					// import the needed namespace for generic List type
					if (variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(List<>))
						targetMethod.Import(typeof(List<>).Namespace);
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
						targetMethod.AddAssignmentInstruction(variableType, variableName, $"new {elementType.GetReadableName()}[{lengthVarName}]", deserializationOption);
					}
					// CODE: List<T> var = new List<T>[length];
					else
					{
						// import the needed namespace for generic List type
						targetMethod.Import(typeof(List<>).Namespace);

						TypeDefinition genericVariableType = TypeDefinition.MakeGenericType(variableType, elementType);

						targetMethod.AddAssignmentInstruction(genericVariableType, variableName, $"new List<{elementType.GetReadableName()}>({lengthVarName})", deserializationOption);
					}

					// CODE: for loop and nested serialization
					string loopVarName = variableName + "_x";
					targetMethod.BeginForIntLoop(loopVarName, "0", lengthVarName);
					AddDeserializationCommands(targetMethod, $"{variableName}[{loopVarName}]", elementType, DeserializationOption.DontAllocateVariable);
					targetMethod.EndLoop();
				}

				return;
			}
			if (variableType.IsGenericType && variableType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				Type keyElementType = variableType.GetGenericArguments()[0];
				Type valElementType = variableType.GetGenericArguments()[1];

				targetMethod.Import(typeof(List<>).Namespace);

				// if both key and value are serializable types, there's a one liner method to serialize it
				if (typeof(ISerializable).IsAssignableFrom(keyElementType) || serializableTypes.Contains(keyElementType) &&
				    typeof(ISerializable).IsAssignableFrom(valElementType) || serializableTypes.Contains(valElementType))
				{
					string types = $"{keyElementType.GetReadableName()},{valElementType.GetReadableName()}";
					targetMethod.AddMethodCallWithAssignment(variableName, $"Dictionary<{types}>", "reader", $"ReadObjectDictionary<{types}>");
				}
				// otherwise, serialize it inline
				else
				{
					// nested-deserialize array
					string lengthVarName = variableName + "_length";
					string loopVarName = variableName + "_x";

					// CODE: int varLength = reader.ReadInt32();
					AddDeserializationCommands(targetMethod, lengthVarName, typeof(int));

					TypeDefinition genericVariableName = TypeDefinition.MakeGenericType(variableType, keyElementType, valElementType);
					targetMethod.AddAssignmentInstruction(genericVariableName, variableName, $"new Dictionary<{keyElementType.GetReadableName()},{valElementType.GetReadableName()}>({lengthVarName})");

					// CODE: foreach loop and nested serialization
					targetMethod.BeginForIntLoop(loopVarName, "0", lengthVarName);
					AddDeserializationCommands(targetMethod, "key", keyElementType, DeserializationOption.AllocateVariable);
					AddDeserializationCommands(targetMethod, "val", valElementType, DeserializationOption.AllocateVariable);
					targetMethod.AddAssignmentInstruction("dic[key]", "val");
					targetMethod.EndLoop();
				}
				return;
			}

			// find primitive type deserializer method within ISignalReader
			SignalReaderMethod paramReadMethod = new SignalReaderMethod(variableType);

			// if it's found, immediately write the command to deserialize it from the signal reader
			if (paramReadMethod.IsAvailable)
			{
				paramReadMethod.AddMethodCall(targetMethod, variableName, deserializationOption);
				return;
			}

			// see if the type is marked for network serialization
			NetworkSerializeAttribute serializeAttribute = variableType.GetCustomAttribute<NetworkSerializeAttribute>();

			// add it to list of types that need to be serialized
			if (serializeAttribute != null)
				AddSerializedType(variableType);

			// could not find a native writer? look for an implementor of ISerializable
			bool typeImplementsSerializable = typeof(ISerializable).IsAssignableFrom(variableType);

 			// write serialization command(s) if it implements ISerializable OR if it's marked for code generation
			if (typeImplementsSerializable || serializeAttribute != null)
			{
				// create the object to serialize the data into
				if (deserializationOption == DeserializationOption.AllocateVariable)
					targetMethod.AddAssignmentInstruction(variableType, variableName, $"new {variableType.Name}()");
				else if (deserializationOption == DeserializationOption.DontAllocateVariable)
					targetMethod.AddAssignmentInstruction(variableName, $"new {variableType}()");
				else
					throw new Exception("Undefined DeserializationOption.");

				// call the deserialize method and read the value into the newly created variable
				targetMethod.AddMethodCall(variableName, "Deserialize", "reader");

				// add to list of type serializers to be generated - ONLY if it doesn't already implement ISerializable
				AddSerializedType(variableType);
				return;
			}

			// failed at this point
			failedTypes[variableType] = SerializationFailureReason.TypeNotSerializable;
		}

		private string GetTypeSerializerClassPath(Type serializableType)
		{
			// name will always be saved with a "_Serialization" suffix
			// string fileName = $"{serializableType.Name}_Serialization";

			string classPath = FileUtility.FindClassPath(serializableType.Name, false);
			
			// make sure original class is partial
			string classText = File.ReadAllText(classPath);
			if (classText.Contains($"class {serializableType.Name}") &&
			    !classText.Contains($"partial class {serializableType.Name}"))
			{
				classText = classText.Replace($"class {serializableType.Name}", $"partial class {serializableType.Name}");
				NSDebug.Log($"Adding partial keyword to the class {serializableType.Name}.");
				File.WriteAllText(classPath, classText);
			}

			return classPath.Replace($"{serializableType.Name}.cs", $"{serializableType.Name}_Serialization.cs");
		}

		public void GenerateTypeSerializers()
		{
			foreach (Type serializableType in serializableTypes)
			{
				ClassDefinition typeSerializerDef = CreateTypeSerializerClass(serializableType);

				string path = GetTypeSerializerClassPath(serializableType);

				string newContent = typeSerializerDef.ToScriptWriter().ToString();
				
				// if file already exists, don't write, and add a helpful log message
				if (File.Exists(path) && File.ReadAllText(path) != newContent)
					NSDebug.Log($"Detected a serialization change in {serializableType.Name} and will not generate serializer. Delete the file '{path}' and try again to re-generate it.");
				else
					File.WriteAllText(path, newContent);
			}
		}

		public ClassDefinition CreateTypeSerializerClass(Type serializableType)
		{
			ClassDefinition typeSerializer = new ClassDefinition(serializableType.Name);
			typeSerializer.type.Namespace = serializableType.Namespace;
			typeSerializer.isPartial = true;

			typeSerializer.interfaces.Add(typeof(ISerializable));

			SerializedMember[] serializeMembers = GetSerializationMembers(serializableType);

			// create WRITER method
			MethodDefinition writerMethod = new MethodDefinition("Serialize");
			writerMethod.Parameters.Add(new ParameterDefinition("writer", typeof(ISignalWriter)));

			foreach (SerializedMember member in serializeMembers)
			{
				AddSerializationCommands(writerMethod.Body, member.name, member.type);
			}

			// create READER method
			MethodDefinition readerMethod = new MethodDefinition("Deserialize");
			readerMethod.Parameters.Add(new ParameterDefinition("reader", typeof(ISignalReader)));

			foreach (SerializedMember member in serializeMembers)
			{
				AddDeserializationCommands(readerMethod.Body, member.name, member.type, DeserializationOption.DontAllocateVariable);
			}

			typeSerializer.methods.Add(writerMethod);
			typeSerializer.methods.Add(readerMethod);

			return typeSerializer;
		}

		private static SerializedMember[] GetSerializationMembers(Type serializableType)
		{
			List<SerializedMember> members = new List<SerializedMember>();

			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
			
			// get all fields and properties
			members.AddRange(serializableType.GetFields(bindingFlags).Select(f => new SerializedMember(f)));
			members.AddRange(serializableType.GetProperties(bindingFlags).Select(f => new SerializedMember(f)));

			// sort and return them
			members = members
				.OrderBy(p => p.serializeAttribute == null)
				.ThenBy(p => p.serializeAttribute?.order)
				.ThenBy(p => p.serializeAttribute?.lineNumber)
				.ToList();
			
			return members.ToArray();
		}

		void AddSerializedType(Type variableType)
		{
			// skip if class already implements ISerializable
			if (typeof(ISerializable).IsAssignableFrom(variableType))
				return;
			
			serializableTypes.Add(variableType);
		}

		public static TypeDefinition GetPromiseType(Type type)
		{
			Type mainPromiseType = (type.Namespace == "System") ? typeof(ValuePromise<>) : typeof(ObjectPromise<>);

			return TypeDefinition.MakeGenericType(mainPromiseType, type.GetReadableName());
		}
	}

	public class SerializedMember
	{
		readonly FieldInfo _field;
		readonly PropertyInfo _property;
		public readonly NetworkPropertyAttribute serializeAttribute;

		public Type type => _field?.FieldType ?? _property.PropertyType;
		public string name => _field?.Name ?? _property.Name;

		private SerializedMember(FieldInfo field, PropertyInfo prop)
		{
			_field = field;
			_property = prop;

			MemberInfo member = field != null ? (MemberInfo)field : prop;
			
			serializeAttribute = member.GetCustomAttribute<NetworkPropertyAttribute>();
		}

		public SerializedMember(FieldInfo field) : this(field, null) {}
		
		public SerializedMember(PropertyInfo property) : this(null, property) {}
	}
}