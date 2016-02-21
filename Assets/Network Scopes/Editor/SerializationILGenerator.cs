using Mono.Cecil.Cil;
using System;

namespace NetworkScopes
{
	using Mono.Cecil;
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Linq;

	public class SerializationILGenerator
	{
		public static MethodDefinition GetOrCreateCustomTypeSerializeMethod(TypeDefinition customTypeDef)
		{
			// find (maybe previously created) serialize method
			MethodDefinition serializeMethod = customTypeDef.Methods.FirstOrDefault(m => m.Name == "NetworkSerialize");

			// if found, return it immediately
			if (serializeMethod != null)
				return serializeMethod;

			// get serialization options
			var serializeConfigAttribute = customTypeDef.CustomAttributes.First(attr => attr.AttributeType.FullName == typeof(NetworkSerialization).FullName);
			NetworkSerializeSettings serializeSettings = (NetworkSerializeSettings)serializeConfigAttribute.ConstructorArguments[0].Value;

			// if the serialization setting is set to custom, look for the method
			if (serializeSettings == NetworkSerializeSettings.Custom)
			{
				Debug.LogErrorFormat("NetworkScopes: Failed to serialize the custom type <color=white>{0}</color> using custom serialize settings because it does not contain the static method NetworkSeserialize({0},NetworkWriter)", customTypeDef.Name);
				throw new Exception("NetworkScope: Terminating scope patching");
			}

			// otherwise, create and add it to the custom type
			// create static NetworkSerialize method
			serializeMethod = new MethodDefinition("NetworkSerialize", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, customTypeDef.Module.Import(typeof(void)));

			// prepare network writer
			TypeReference netWriterRef = customTypeDef.Module.Import(typeof(NetworkWriter));
			TypeDefinition netWriterDef = netWriterRef.Resolve();

			// here comes the code injection
			ILProcessor il = serializeMethod.Body.GetILProcessor();

			// serialize fields
			foreach (FieldDefinition serializedField in customTypeDef.Fields)
			{
				// ignore backing fields
				if (serializedField.Name.Contains("k__BackingField"))
					continue;

				// skip non-public fields if specified
				if ((!serializedField.IsPublic && serializeSettings == NetworkSerializeSettings.PublicFieldsOnly) ||
					serializeSettings == NetworkSerializeSettings.OptIn)
				{
					// don't skip it if it has an explicit NetworkSerialized attribute
					if (!serializedField.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkSerialized).FullName))
						continue;
				}

				// also skip if it has an explicit NetworkNonSerialized attribute
				if (serializedField.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkNonSerialized).FullName))
					continue;

				// load NetworkWriter
				il.Emit(OpCodes.Ldarg_1);

				// load custom object containing the field
				il.Emit(OpCodes.Ldarg_0);

				// load the field
				il.Emit(OpCodes.Ldfld, serializedField);

				// find the NetworkWriter.WriteX method
				MethodDefinition netWriterWriteMethodDef = netWriterDef.Methods.FirstOrDefault(m => m.Name == "Write" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == serializedField.FieldType.FullName);

				if (netWriterWriteMethodDef == null)
				{
					Debug.Log("GET underlying of type " + serializedField.FieldType.GetElementType().Name + " of orig " + serializedField.FieldType.Name);
					// try to read underlying enum type
					FieldDefinition underlyingEnumField = serializedField.FieldType.GetElementType().Resolve().Fields.FirstOrDefault(f => f.Name == "value__");

					// if it's an enum, serialize it as its underlying type
					if (underlyingEnumField != null)
					{
						netWriterWriteMethodDef = netWriterDef.Methods.FirstOrDefault(m =>
							m.Name == "Write" &&
							m.Parameters.Count == 1 &&
							m.Parameters[0].ParameterType.FullName == underlyingEnumField.FieldType.FullName);
					}

					Debug.Log("FOUND! " + underlyingEnumField);
				}

				// call NetworkWriter.Write(X)
				il.Emit(OpCodes.Callvirt, customTypeDef.Module.Import(netWriterWriteMethodDef));

			}

			// serialize properties if specified
			foreach (PropertyDefinition serializedProperty in customTypeDef.Properties)
			{
				// skip if specified by the master settings
				if ((serializeSettings != NetworkSerializeSettings.PublicFieldsAndProperties &&
					serializeSettings != NetworkSerializeSettings.AllFieldsAndProperties) ||
					serializeSettings == NetworkSerializeSettings.OptIn)
				{
					// ...unless it has an explicit NetworkSerialized attribute
					if (!serializedProperty.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkSerialized).FullName))
						continue;
				}

				// also skip if it has an explicit NetworkNonSerialized attribute
				if (serializedProperty.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkNonSerialized).FullName))
					continue;

				// load NetworkWriter
				il.Emit(OpCodes.Ldarg_1);

				// load custom object containing the field
				il.Emit(OpCodes.Ldarg_0);

				// call the property's getter method
				il.Emit(OpCodes.Callvirt, serializedProperty.GetMethod);

				// find the NetworkWriter.WriteX method
				MethodDefinition netWriterWriteMethodDef = netWriterDef.Methods.First(m => m.Name == "Write" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == serializedProperty.PropertyType.FullName);

				// call NetworkWriter.Write(X)
				il.Emit(OpCodes.Callvirt, customTypeDef.Module.Import(netWriterWriteMethodDef));
			}

			il.Emit(OpCodes.Ret);

			ParameterDefinition valueParam = new ParameterDefinition(customTypeDef);
			ParameterDefinition writerParam = new ParameterDefinition(netWriterRef);

			serializeMethod.Parameters.Add(valueParam);
			serializeMethod.Parameters.Add(writerParam);

			// add it to the parameter type to be custom serialized
			customTypeDef.Methods.Add(serializeMethod);

			return serializeMethod;
		}

		public static MethodDefinition GetOrCreateCustomTypeDeserializeMethod(TypeDefinition customTypeDef)
		{
			// find (maybe previously created) serialize method
			MethodDefinition deserializeMethod = customTypeDef.Methods.FirstOrDefault(m => m.Name == "NetworkDeserialize" && m.IsStatic);

			// if found, return it immediately
			if (deserializeMethod != null)
				return deserializeMethod;

			// get serialization options
			var serializeConfigAttribute = customTypeDef.CustomAttributes.First(attr => attr.AttributeType.FullName == typeof(NetworkSerialization).FullName);
			NetworkSerializeSettings serializeSettings = (NetworkSerializeSettings)serializeConfigAttribute.ConstructorArguments[0].Value;

			// if the serialization setting is set to custom, look for the method
			if (serializeSettings == NetworkSerializeSettings.Custom)
			{
				Debug.LogErrorFormat("NetworkScopes: Failed to serialize the custom type <color=white>{0}</color> using custom serialize settings because it does not contain the static method NetworkDeserialize({0},NetworkReader)", customTypeDef.Name);
				throw new Exception("NetworkScope: Terminating scope patching");
			}

			// otherwise create and inject the method into the custom serialized type
			deserializeMethod = new MethodDefinition("NetworkDeserialize", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, customTypeDef.Module.Import(typeof(void)));

			// prepare network writer
			TypeReference netReaderRef = customTypeDef.Module.Import(typeof(NetworkReader));
			TypeDefinition netReaderDef = netReaderRef.Resolve();

			// here comes the code injection
			ILProcessor il = deserializeMethod.Body.GetILProcessor();

			// deserialize fields
			foreach (FieldDefinition deserializedField in customTypeDef.Fields)
			{
				// ignore backing fields
				if (deserializedField.Name.Contains("k__BackingField"))
					continue;

				// skip non-public fields if specified
				if ((!deserializedField.IsPublic && serializeSettings == NetworkSerializeSettings.PublicFieldsOnly) ||
					serializeSettings == NetworkSerializeSettings.OptIn)
				{
					// don't skip it if it has an explicit NetworkSerialized attribute
					if (!deserializedField.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkSerialized).FullName))
						continue;
				}

				// also skip if it has an explicit NetworkNonSerialized attribute
				if (deserializedField.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkNonSerialized).FullName))
					continue;

				// load custom object containing the field
				il.Emit(OpCodes.Ldarg_0);

				// load NetworkReader
				il.Emit(OpCodes.Ldarg_1);

				// find the NetworkWriter.ReadX method
				MethodDefinition netReaderReadMethodDef = netReaderDef.Methods.First(m => m.Name.StartsWith("Read") && m.ReturnType.FullName == deserializedField.FieldType.FullName);

				// call NetworkReader.ReadX
				il.Emit(OpCodes.Callvirt, customTypeDef.Module.Import(netReaderReadMethodDef));

				// store it in our object's field
				il.Emit(OpCodes.Stfld, deserializedField);
			}

			// deserialize properties if specified
			foreach (PropertyDefinition deserializedProperty in customTypeDef.Properties)
			{
				if ((serializeSettings != NetworkSerializeSettings.PublicFieldsAndProperties && serializeSettings != NetworkSerializeSettings.AllFieldsAndProperties) ||
					serializeSettings == NetworkSerializeSettings.OptIn)
				{
					// don't skip it if it has an explicit NetworkSerialized attribute
					if (!deserializedProperty.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkSerialized).FullName))
						continue;
				}

				// also skip if it has an explicit NetworkNonSerialized attribute
				if (deserializedProperty.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(NetworkNonSerialized).FullName))
					continue;

				// load custom object containing the field
				il.Emit(OpCodes.Ldarg_0);

				// load NetworkReader
				il.Emit(OpCodes.Ldarg_1);

				// find the NetworkWriter.ReadX method
				MethodDefinition netReaderReadMethodDef = netReaderDef.Methods.First(m => m.Name.StartsWith("Read") && m.ReturnType.FullName == deserializedProperty.PropertyType.FullName);

				// call NetworkReader.ReadX
				il.Emit(OpCodes.Callvirt, customTypeDef.Module.Import(netReaderReadMethodDef));

				// store it in our object's field
				il.Emit(OpCodes.Callvirt, deserializedProperty.SetMethod);
			}

			il.Emit(OpCodes.Ret);

			ParameterDefinition valueParam = new ParameterDefinition(customTypeDef);
			ParameterDefinition readerParam = new ParameterDefinition(customTypeDef.Module.Import(typeof(NetworkReader)));

			deserializeMethod.Parameters.Add(valueParam);
			deserializeMethod.Parameters.Add(readerParam);

			// add it to the parameter type to be custom serialized
			customTypeDef.Methods.Add(deserializeMethod);

			return deserializeMethod;
		}
	}
}