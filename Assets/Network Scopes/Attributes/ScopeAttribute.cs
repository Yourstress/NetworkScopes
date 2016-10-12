
namespace NetworkScopes
{
	using System;
	using System.Reflection;
	using CodeGeneration;

	public class ScopeAttribute : InjectAttribute
	{
		Type remoteType;

		public ScopeAttribute(Type remoteType)
		{
			this.remoteType = remoteType;
		}

		public override void ProcessClass(Type classType, ClassDefinition classDef)
		{
			MethodInfo[] remoteMethods = remoteType.GetMethods();

			bool isServer = !classType.IsSubclassOf(typeof(ClientScope));

			// create and setup writer
			ClassDefinition fakeRemoteClass = new ClassDefinition(string.Format("Remote{0}", remoteType.Name));
			{
					
				fakeRemoteClass.imports.Add("System.Collections.Generic");
				fakeRemoteClass.imports.Add("NetworkScopes");
				fakeRemoteClass.imports.Add("UnityEngine.Networking");
				
				// add interface sender field
				fakeRemoteClass.AddField("_netSender", "INetworkSender", false);
				
				// add constructor method
				MethodDefinition ctor = new MethodDefinition(fakeRemoteClass.Name);
				ctor.IsConstructor = true;
				ctor.parameters.Add(new ParameterDefinition("netSender", "INetworkSender"));
				fakeRemoteClass.methods.Add(ctor);

				// assign it in the body
				ctor.instructions.AddInstruction("_netSender = netSender;");
			}


			// find remote methods with "Signal" attribute; the SEND methods
			bool didCreateAnySendMethods = false;
			foreach (MethodInfo remoteMethod in remoteMethods)
			{
				if (ReflectionUtility.ContainsAttribute<Signal>(remoteMethod))
				{
					// create send remote method
					MethodDefinition fakeMethodDef = CreateSendMethod(remoteMethod);

					// if method call wasn't found, mark it invalid
					if (fakeMethodDef == null)
					{
						classDef.IsInvalid = true;
						continue;
					}
					// otherwise add it to the list of methods in the inner remote class
					fakeRemoteClass.methods.Add(fakeMethodDef);

					// flag that send methods were created to allow the class to be created in a future check
					didCreateAnySendMethods = true;
				}
			}

			// find local methods with "Signal" attribute; the RECEIVE methods
			foreach (MethodInfo localMethod in classType.GetMethods())
			{
				if (ReflectionUtility.ContainsAttribute<Signal>(localMethod))
				{
					// create send remote method
					MethodDefinition localMethodDef = CreateReceiveMethod(localMethod);

					// if method call wasn't found, mark it invalid
					if (localMethodDef == null)
						classDef.IsInvalid = true;
					// otherwise add it to the lsit of methods within the partial class
					else
						classDef.methods.Add(localMethodDef);
				}
			}

			// add SEND-specific code - ONLY if there's any any send methods generated earlier
			if (didCreateAnySendMethods)
			{
				classDef.classes.Add(fakeRemoteClass);

				if (isServer)
				{
					string peerTypeName = classType.BaseType.GetGenericArguments()[0].FullName;

					classDef.AddField("_Remote", fakeRemoteClass.Name, false);

					// send method (single-peer)
					MethodDefinition sendPeerMethod = new MethodDefinition("SendToPeer");
					sendPeerMethod.ReturnType = fakeRemoteClass.Name;
					sendPeerMethod.parameters.Add(new ParameterDefinition("targetPeer", peerTypeName));

					sendPeerMethod.instructions.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					sendPeerMethod.instructions.AddInstruction("TargetPeer = targetPeer;");
					sendPeerMethod.instructions.AddInstruction("return _Remote;");

					classDef.methods.Add(sendPeerMethod);

					// reply method (single-peer - last sender)
					sendPeerMethod = new MethodDefinition("ReplyToPeer");
					sendPeerMethod.ReturnType = fakeRemoteClass.Name;

					sendPeerMethod.instructions.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					sendPeerMethod.instructions.AddInstruction("TargetPeer = SenderPeer;");
					sendPeerMethod.instructions.AddInstruction("return _Remote;");

					classDef.methods.Add(sendPeerMethod);

					// send method (multi-peer)
					sendPeerMethod = new MethodDefinition("SendToPeers");
					sendPeerMethod.ReturnType = fakeRemoteClass.Name;
					sendPeerMethod.parameters.Add(new ParameterDefinition("targetPeerGroup", string.Format("IEnumerable<{0}>", peerTypeName)));

					sendPeerMethod.instructions.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					sendPeerMethod.instructions.AddInstruction("TargetPeerGroup = targetPeerGroup;");
					sendPeerMethod.instructions.AddInstruction("return _Remote;");

					classDef.methods.Add(sendPeerMethod);
				}
				else
				{
					// add the 'Server' property by that type
					classDef.AddField("_Remote", fakeRemoteClass.Name, false);
					var prop = classDef.AddProperty("SendToServer", fakeRemoteClass.Name);
					prop.getterBody = new MethodBodyDefinition();

					prop.getterBody.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					prop.getterBody.AddInstruction("return _Remote;");
				}
			}
		}

		private MethodDefinition CreateReceiveMethod(MethodInfo localMethod)
		{
			MethodDefinition receiveMethodDef = new MethodDefinition("Receive_" +  localMethod.Name);

			// only has one parameter: NetworkReader
			receiveMethodDef.parameters.Add(new ParameterDefinition("reader", NetworkScopeUtility.readerType));

			ParameterInfo[] paramInfos = localMethod.GetParameters();

			for (int x = 0; x < paramInfos.Length; x++)
			{
				AddDeserializeCode(receiveMethodDef, paramInfos[x].Name, paramInfos[x].ParameterType, localMethod);
			}

			// when deserializing within the local type, call the receiving method
			if (localMethod != null)
			{
				receiveMethodDef.instructions.AddInstruction("{0}({1});", localMethod.Name, string.Join(", ", Array.ConvertAll(paramInfos, pi => pi.Name)));
			}

			return receiveMethodDef;
		}

		private MethodDefinition CreateSendMethod(MethodInfo remoteMethod)
		{
			MethodDefinition fakeMethodDef = new MethodDefinition(remoteMethod);

			// create writer variable
//			fakeMethodDef.instructions.AddVariableInstruction("writer", NetworkReflector.writerType, NetworkReflector.writerCtor);
			fakeMethodDef.instructions.AddInstruction("NetworkWriter writer = _netSender.CreateWriter({0});", remoteMethod.Name.GetHashCode());

			ParameterInfo[] paramInfos = remoteMethod.GetParameters();

			for (int x = 0; x < fakeMethodDef.parameters.Count; x++)
			{
				ParameterDefinition paramDef = fakeMethodDef.parameters[x];

				Type paramType = paramInfos[x].ParameterType;

				AddSerializeCode(fakeMethodDef, paramDef.Name, paramType);
			}

			// send it out
			fakeMethodDef.instructions.AddInstruction("_netSender.PrepareAndSendWriter(writer);");

			return fakeMethodDef;
		}

		private bool AddSerializeCode(MethodDefinition method, string paramName, Type paramType)
		{
			Type elementType = paramType;

			if (elementType.IsEnum)
				elementType = Enum.GetUnderlyingType(elementType);

			// get a method out of NetworkWriter that can serialize the parameter type
			MethodInfo typeSerializer = NetworkScopeUtility.GetNetworkWriterSerializer(elementType);

			// if this type is an array, do some array magic
			if (elementType.IsArray)
			{
				// write array count
				method.instructions.AddInstruction("writer.Write({0}.Length);", paramName);
				string arrCounterName = "_arrCounter";
				method.instructions.AddInstruction("for (int {0} = 0; {0} < {1}.Length; {0}++)", arrCounterName, paramName);

				return AddSerializeCode(method, string.Format("{0}[{1}]", paramName, arrCounterName), elementType.GetElementType());
			}

			// write out the NetworkWriter serializer call
			if (typeSerializer != null)
			{
				if (elementType != paramType)
					method.instructions.AddMethodCall("writer", typeSerializer, string.Format("({0}){1}", elementType, paramName));
				else
					method.instructions.AddMethodCall("writer", typeSerializer, paramName);
				return true;
			}

			// if type can't be serialized by NetworkWriter directly, see if the type has a serializer/deserializer method
			if (typeSerializer == null)
				typeSerializer = NetworkScopeUtility.GetCustomTypeSerializer(elementType);

			if (typeSerializer != null)
			{
				// call the static NetworkSerialize method
				method.instructions.AddMethodCall(elementType.Name, typeSerializer, paramName, "writer");
				return true;
			}

			// attempt to create static serializer if attributed with NetworkSerialization
			ClassDefinition autoGeneratedSerializer = CreateStaticSerializerClass(paramType);
			if (autoGeneratedSerializer != null)
			{
				method.instructions.AddInstruction("NetworkSerializer.{0}.NetworkSerialize({1}, writer);", autoGeneratedSerializer.Name, paramName);

				return true;
			}

			// can't find serializer? exit out
			UnityEngine.Debug.LogWarningFormat("Could not find a serializer for the type '{0}'", elementType.FullName);

			return false;
		}

		// when localMethod is null, it means serialization is happening within a custom-serialized class
		private bool AddDeserializeCode(MethodDefinition method, string paramName, Type paramType, MethodInfo localMethod)
		{
			Type elementType = paramType;

			if (elementType.IsEnum)
				elementType = Enum.GetUnderlyingType(elementType);
					
			// get a method out of NetworkWriter that can serialize the parameter type
			MethodInfo typeDeserializer = NetworkScopeUtility.GetNetworkReaderDeserializer(elementType);

			// if this type is an array, do some array magic
			if (elementType.IsArray)
			{
				string arrCount = string.Format("{0}_count", paramName);
				string arrIndex = string.Format("{0}_index", paramName);

				// read count
				method.instructions.AddInstruction("Int32 {0} = reader.ReadInt32();", arrCount);

				// define variable if deserializing local method
				if (localMethod != null)
					method.instructions.AddInstruction("{0}[] {1} = new {0}[{2}];", elementType.GetElementType(), paramName, arrCount);

				method.instructions.AddInstruction("for (int {0} = 0; {0} < {1}; {0}++)", arrIndex, arrCount);

				return AddDeserializeCode(method, string.Format("{0}[{1}]", paramName, arrIndex), elementType.GetElementType(), null);
			}

			// write out the NetworkWriter serializer call
			if (typeDeserializer != null)
			{
				// create new variable if specified
//				string createVarModifier = (localMethod != null) ? string.Format("{0} ", paramType) : string.Empty;
				bool cast = paramType != elementType;

				// call the reader function and assign the value
				method.instructions.AddMethodCallWithAssignment(paramName, paramType, "reader", cast, typeDeserializer, localMethod != null);

				return true;
			}

			// if type can't be serialized by NetworkWriter directly, see if the type has a serializer/deserializer method
			if (typeDeserializer == null)
				typeDeserializer = NetworkScopeUtility.GetCustomTypeDeserializer(elementType);

			// define variable if we're in the local scope
			if (localMethod != null)
				method.instructions.AddInstruction("{0} {1} = new {0}();", elementType, paramName);

			if (typeDeserializer != null)
			{	
				// call the static NetworkSerialize method
				method.instructions.AddMethodCall(elementType.Name, typeDeserializer, paramName, "reader");
				return true;
			}

			// attempt to create static serializer if attributed with NetworkSerialization
			ClassDefinition autoGeneratedSerializer = CreateStaticSerializerClass(paramType);
			if (autoGeneratedSerializer != null)
			{
				method.instructions.AddInstruction("NetworkSerializer.{0}.NetworkDeserialize({1}, reader);", autoGeneratedSerializer.Name, paramName);

				return true;
			}

			// can't find serializer? exit out
			UnityEngine.Debug.LogWarningFormat("Could not find a deserializer for the type '{0}'", elementType.FullName);

			return false;
		}

		private ClassDefinition CreateStaticSerializerClass(Type paramType)
		{
			string serializerClassName = string.Format("{0}Serializer", paramType.FullName.Replace(".",string.Empty));

			// see if it exists in the serializer class
			ClassDefinition serializerClassDef = NetworkScopeUtility.SerializerClass.classes.Find(cd => cd.Name == serializerClassName);

			if (serializerClassDef != null)
				return serializerClassDef;

			NetworkSerialization serializationAttr = null;
			if (serializerClassDef == null && (serializationAttr = ReflectionUtility.GetAttribute<NetworkSerialization>(paramType)) != null)
			{
				// create and add the serializer class for this type
				serializerClassDef = new ClassDefinition(serializerClassName);
				serializerClassDef.IsStatic = true;
				NetworkScopeUtility.SerializerClass.classes.Add(serializerClassDef);

				// serializer method
				{
					MethodDefinition serializerMethod = new MethodDefinition("NetworkSerialize");
					serializerMethod.IsStatic = true;
					serializerMethod.parameters.Add(new ParameterDefinition("value", paramType));
					serializerMethod.parameters.Add(new ParameterDefinition("writer", typeof(UnityEngine.Networking.NetworkWriter)));

					serializerClassDef.methods.Add(serializerMethod);

					FieldInfo[] serializeFields;
					PropertyInfo[] serializeProps;
					serializationAttr.GetSerializedMembers(paramType, out serializeFields, out serializeProps);


					if (serializeFields != null)
						foreach (FieldInfo field in serializeFields)
							if (!field.Name.EndsWith("k__BackingField"))		// ignore property backing fields
								AddSerializeCode(serializerMethod, string.Format("value.{0}", field.Name), field.FieldType);
					if (serializeProps != null)
						foreach (PropertyInfo prop in serializeProps)
							AddSerializeCode(serializerMethod, string.Format("value.{0}", prop.Name), prop.PropertyType);
				}

				// deserializer method
				{
					MethodDefinition serializerMethod = new MethodDefinition("NetworkDeserialize");
					serializerMethod.IsStatic = true;
					serializerMethod.parameters.Add(new ParameterDefinition("value", paramType));
					serializerMethod.parameters.Add(new ParameterDefinition("reader", typeof(UnityEngine.Networking.NetworkReader)));

					serializerClassDef.methods.Add(serializerMethod);

					FieldInfo[] serializeFields;
					PropertyInfo[] serializeProps;
					serializationAttr.GetSerializedMembers(paramType, out serializeFields, out serializeProps);


					if (serializeFields != null)
						foreach (FieldInfo field in serializeFields)
							if (!field.Name.EndsWith("k__BackingField"))		// ignore property backing fields
								AddDeserializeCode(serializerMethod, string.Format("value.{0}", field.Name), field.FieldType, null);
					if (serializeProps != null)
						foreach (PropertyInfo prop in serializeProps)
							AddDeserializeCode(serializerMethod, string.Format("value.{0}", prop.Name), prop.PropertyType, null);
				}

				return serializerClassDef;
			}

			return null;
		}
	}
}