using Lidgren.Network;
using UnityEngine;

namespace NetworkScopes
{
	using System;
	using System.Linq;
	using System.Reflection;
	using CodeGeneration;
	using NetworkScopes.CodeProcessors;

	public class ScopeAttribute : InjectAttribute
	{
		Type remoteType;

		public ScopeAttribute(Type remoteType)
		{
			this.remoteType = remoteType;
		}

		public override void ProcessClass(Type classType, ClassDefinition classDef)
		{
			// find remote methods
			MethodInfo[] remoteMethods = remoteType.GetMethods();

			// find local & remote NetworkObjects
			FieldInfo[] localNetObjFields = NetworkVariableProcessor.GetNetworkVariableFields(classType, true).ToArray();
			FieldInfo[] remoteNetObjFields = NetworkVariableProcessor.GetNetworkVariableFields(remoteType, true).ToArray();

			bool isServer = !classType.IsSubclassOf(typeof(ClientScope));

			string peerTypeName = null;

			if (isServer)
			{
				var baseType = classType.BaseType;

				// if this class does not directly inherit from ServerScope...
				while (!baseType.Name.StartsWith("ServerScope"))
					baseType = baseType.BaseType;

				Type peerType = baseType.GetGenericArguments()[0];

				peerTypeName = peerType.Name;

				classDef.Import(peerType);
			}

			// create and setup writer
			ClassDefinition fakeRemoteClass = new ClassDefinition(string.Format("Remote{0}", remoteType.Name));
			{
				fakeRemoteClass.imports.Add("System.Collections.Generic");
				fakeRemoteClass.imports.Add("NetworkScopes");
				fakeRemoteClass.imports.Add("Lidgren.Network");

				string msgSenderType = isServer ? $"IPeerMessageSender<{peerTypeName}>" : "IMessageSender";

				// add interface sender field
				fakeRemoteClass.AddField("_netSender", msgSenderType, false);

				// add constructor method
				MethodDefinition ctor = new MethodDefinition(fakeRemoteClass.Name);
				ctor.IsConstructor = true;
				ctor.parameters.Add(new ParameterDefinition("netSender", msgSenderType));
				fakeRemoteClass.methods.Add(ctor);

				// assign it in the body
				ctor.instructions.AddInstruction("_netSender = netSender;");
			}

			// local has fields? add registration code to scope
			if (localNetObjFields.Length > 0)
				NetworkVariableProcessor.GenerateNetworkVariableCode(classDef, localNetObjFields, true);

			// remote has fields? add registration code to scope and create local fields
			if (remoteNetObjFields.Length > 0)
				NetworkVariableProcessor.GenerateNetworkVariableCode(classDef, remoteNetObjFields, false);

			// find remote methods with "Signal" attribute; the SEND methods
			bool didCreateAnySendMethods = false;
			bool containsTwoWaySignals = false;
			foreach (MethodInfo remoteMethod in remoteMethods)
			{
				if (ReflectionUtility.ContainsAttribute<Signal>(remoteMethod))
				{
					// create send remote method
					MethodDefinition fakeMethodDef = CreateSendMethod(remoteMethod, isServer, peerTypeName);

					// if send method has a SignalResponse (return value) create the receive response method
					if (remoteMethod.ReturnType != typeof(void))
					{
					 	MethodDefinition recvResponseMethod = CreateReceiveResponseMethod(remoteMethod, isServer);
						classDef.methods.Add(recvResponseMethod);
					}

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

					if (!containsTwoWaySignals && remoteMethod.ReturnType != typeof(void))
						containsTwoWaySignals = true;
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

					// send method (multi-peer with exception)
					sendPeerMethod = new MethodDefinition("SendToPeersExcept");
					sendPeerMethod.ReturnType = fakeRemoteClass.Name;
					sendPeerMethod.parameters.Add(new ParameterDefinition("targetPeerGroup", string.Format("IEnumerable<{0}>", peerTypeName)));
					sendPeerMethod.parameters.Add(new ParameterDefinition("exceptPeer", peerTypeName));

					sendPeerMethod.instructions.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					sendPeerMethod.instructions.AddInstruction("TargetPeerGroup = targetPeerGroup.Where(p => p != exceptPeer);");
					sendPeerMethod.instructions.AddInstruction("return _Remote;");

					classDef.imports.Add("System.Linq");
					classDef.methods.Add(sendPeerMethod);

					// send method (all peers with exception)
					sendPeerMethod = new MethodDefinition("SendToAllExcept");
					sendPeerMethod.ReturnType = fakeRemoteClass.Name;
					sendPeerMethod.parameters.Add(new ParameterDefinition("exceptPeer", peerTypeName));

					sendPeerMethod.instructions.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					sendPeerMethod.instructions.AddInstruction("TargetPeerGroup = Peers.Where(p => p != exceptPeer);");
					sendPeerMethod.instructions.AddInstruction("return _Remote;");

					classDef.imports.Add("System.Linq");
					classDef.methods.Add(sendPeerMethod);

					// if we're capable of two-way signals, add remote response tasks
					if (containsTwoWaySignals)
					{
						string peerRespTypeName = $"Peer{typeof(SignalResponseTasks).Name}<{peerTypeName}>";
						FieldDefinition field = new FieldDefinition("remoteResponseTasks", peerRespTypeName, true);
						field.IsReadonly = true;
						field.assignment = $"new {peerRespTypeName}()";
						fakeRemoteClass.fields.Add(field);
					}
				}
				else
				{
					// add the 'Server' property by that type
					classDef.AddField("_Remote", fakeRemoteClass.Name, false);
					var prop = classDef.AddProperty("SendToServer", fakeRemoteClass.Name);
					prop.getterBody = new MethodBodyDefinition();

					prop.getterBody.AddInlineIfCheck("_Remote == null", string.Format("_Remote = new {0}(this);", fakeRemoteClass.Name));
					prop.getterBody.AddInstruction("return _Remote;");

					// if we're capable of two-way signals, add remote response tasks
					if (containsTwoWaySignals)
					{
						FieldDefinition field = new FieldDefinition("remoteResponseTasks", typeof(SignalResponseTasks).Name, true);
						field.IsReadonly = true;
						field.assignment = $"new {typeof(SignalResponseTasks).Name}()";
						fakeRemoteClass.fields.Add(field);
					}
				}
			}
		}

		private MethodDefinition CreateReceiveResponseMethod(MethodInfo remoteMethod, bool isServer)
		{
			MethodDefinition responseMethodDef = new MethodDefinition("Response_" + remoteMethod.Name);

			SignalResponseData signalResponseData = new SignalResponseData(remoteMethod);

			signalResponseData.ImportTypesInto(responseMethodDef);

			// only has one parameter: NetworkReader
			responseMethodDef.parameters.Add(new ParameterDefinition("reader", NetworkScopeUtility.readerType));

			IVariableSerialization serialization = NetworkVariableProcessor.GetVariableSerialization(signalResponseData.responseType, false, NetworkVariableProcessor.VariableType.Auto);

			int signalType = responseMethodDef.Name.GetConsistentHashCode();
			string optionalPeerParameter = isServer ? "SenderPeer, " : "";

			// _Remote.remoteResponseTasks.DequeueResponseObject<Computer>(SenderPeer, -844153654, reader, Computer.NetworkDeserialize);

			string deserializeMethodSignature = serialization.CreateNetworkDeserializationMethodSignature();
			string methodName = serialization.isValueType
				? "DequeueResponseValue"
				: $"DequeueResponseObject<{signalResponseData.ResponseTypeName}>";

			string dequeueInstruction =
				$"_Remote.remoteResponseTasks.{methodName}({optionalPeerParameter}{signalType}, reader, {deserializeMethodSignature});";

			responseMethodDef.instructions.AddInstruction(dequeueInstruction);

			return responseMethodDef;
		}

		private MethodDefinition CreateReceiveMethod(MethodInfo localMethod)
		{
			MethodDefinition receiveMethodDef = new MethodDefinition("Receive_" + localMethod.Name);

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
				bool isTwoWaySignal = localMethod.ReturnType != typeof(void);

				// if this method is returning something to the other side, put it in a var
				if (isTwoWaySignal)
				{
					SignalResponseData signalResponseData = new SignalResponseData(localMethod);

					signalResponseData.ImportTypesInto(receiveMethodDef);

					if (signalResponseData.isTask)
					{
						receiveMethodDef.IsAsync = true;
					}
					// call receiving method, and store its return value
					receiveMethodDef.instructions.AddInstruction($"{signalResponseData.ResponseTypeName} signalResponse = {(signalResponseData.isTask ? "await " : "")}{localMethod.Name}({string.Join(", ", Array.ConvertAll(paramInfos, pi => pi.Name))});");

					int signalType = $"Response_{localMethod.Name}".GetConsistentHashCode();

					receiveMethodDef.Import(typeof(NetworkScopes.Signal));

					// send out send the returned value
					IVariableSerialization serialization = NetworkVariableProcessor.GetVariableSerialization(signalResponseData.responseType, false, NetworkVariableProcessor.VariableType.Auto);
					receiveMethodDef.instructions.AddInstruction($"this.SendRaw({signalType}, signalResponse, {serialization.CreateNetworkSerializationMethodSignature()});");
				}
				else
				{
					// call receiving method
					receiveMethodDef.instructions.AddInstruction("{0}({1});", localMethod.Name, string.Join(", ", Array.ConvertAll(paramInfos, pi => pi.Name)));
				}
			}

			return receiveMethodDef;
		}

		private MethodDefinition CreateSendMethod(MethodInfo remoteMethod, bool isServer, string peerTypeName)
		{
			MethodDefinition fakeMethodDef = new MethodDefinition(remoteMethod);

			// create writer variable
			//			fakeMethodDef.instructions.AddVariableInstruction("writer", NetworkReflector.writerType, NetworkReflector.writerCtor);
			fakeMethodDef.instructions.AddInstruction("NetOutgoingMessage writer = _netSender.CreateWriter({0});", remoteMethod.Name.GetConsistentHashCode());

			ParameterInfo[] paramInfos = remoteMethod.GetParameters();

			for (int x = 0; x < fakeMethodDef.parameters.Count; x++)
			{
				ParameterDefinition paramDef = fakeMethodDef.parameters[x];

				Type paramType = paramInfos[x].ParameterType;

				AddSerializeCode(fakeMethodDef, paramDef.Name, paramType);
			}

			// send it out
			fakeMethodDef.instructions.AddInstruction("_netSender.PrepareAndSendWriter(writer);");

			// if remote method has a return type, it's a two-way signal that can be awaited
			bool isTwoWaySignal = remoteMethod.ReturnType != typeof(void);
			if (isTwoWaySignal)
			{
				SignalResponseData signalResponseData = new SignalResponseData(remoteMethod);

				fakeMethodDef.Import(signalResponseData.responseType);

				int responseSignal = $"Response_{remoteMethod.Name}".GetConsistentHashCode();

				// when creating a two-way SERVER send method, we have to add the Peer as a parameter
				if (isServer)
				{
					fakeMethodDef.instructions.AddInstruction($"return remoteResponseTasks.EnqueueResponseTask<{signalResponseData.ResponseTypeName}>(_netSender.TargetPeer, {responseSignal});");
				}
				else
				{
					fakeMethodDef.instructions.AddInstruction($"return remoteResponseTasks.EnqueueResponseTask<{signalResponseData.ResponseTypeName}>({responseSignal});");
				}

				fakeMethodDef.ReturnType = $"{typeof(NetworkTask).Name}<{signalResponseData.ResponseTypeName}>";
			}

			return fakeMethodDef;
		}

		private bool AddSerializeCode(MethodDefinition method, string paramName, Type paramType)
		{
			Type elementType = paramType;

			if (elementType.IsEnum)
				elementType = Enum.GetUnderlyingType(elementType);

			// get a method out of NetworkWriter that can serialize the parameter type
			MethodInfo writerMethod = NetworkScopeUtility.GetNetworkWriterSerializer(elementType);

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
			if (writerMethod != null)
			{
				if (elementType != paramType)
					method.instructions.AddMethodCall("writer", writerMethod, string.Format("({0}){1}", elementType, paramName));
				else
					method.instructions.AddMethodCall("writer", writerMethod, paramName);
				return true;
			}

			// if type can't be serialized by NetworkWriter directly, see if the type has a serializer/deserializer method
			ObjectSerialization objectSerialization = ObjectSerialization.FromType(elementType, false);
			if (objectSerialization != null)
				writerMethod = objectSerialization.serializeMethod;

			if (writerMethod != null)
			{
				// call the static NetworkSerialize method
				method.instructions.AddMethodCall(elementType.Name, writerMethod, paramName, "writer");
				return true;
			}

			// attempt to create static serializer if attributed with NetworkSerialization
			ClassDefinition autoGeneratedSerializer = CreateStaticSerializerClass(paramType);
			if (autoGeneratedSerializer != null)
			{
				method.instructions.AddInstruction("Serializers.{0}.NetworkSerialize({1}, writer);", autoGeneratedSerializer.Name, paramName);

				return true;
			}

			// can't find serializer? exit out
			Debug.LogWarningFormat("Could not find a serializer for the type '{0}'", elementType.FullName);

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
				method.instructions.AddInstruction("{");

				Type arrayElemType = elementType.GetElementType();
				if (!arrayElemType.IsValueType)
				{
					method.instructions.AddInstruction("{2}[{0}] = new {1}();", arrIndex, arrayElemType, paramName);
				}

				bool didSerialize = AddDeserializeCode(method, string.Format("{0}[{1}]", paramName, arrIndex), elementType.GetElementType(), null);
				method.instructions.AddInstruction("}");

				return didSerialize;
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
			ObjectSerialization objectSerialization = ObjectSerialization.FromType(elementType, false);
			if (objectSerialization != null)
				typeDeserializer = objectSerialization.deserializeMethod;

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
				method.instructions.AddInstruction("Serializers.{0}.NetworkDeserialize({1}, reader);", autoGeneratedSerializer.Name, paramName);

				return true;
			}

			// can't find serializer? exit out
			Debug.LogWarningFormat("Could not find a deserializer for the type '{0}'", elementType.FullName);

			return false;
		}

		private ClassDefinition CreateStaticSerializerClass(Type paramType)
		{
			string serializerClassName = string.Format("{0}Serializer", paramType.Name.Replace(".", string.Empty));

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
					serializerMethod.parameters.Add(new ParameterDefinition("writer", typeof(NetOutgoingMessage)));

					serializerClassDef.methods.Add(serializerMethod);

					FieldInfo[] serializeFields;
					PropertyInfo[] serializeProps;
					serializationAttr.GetSerializedMembers(paramType, out serializeFields, out serializeProps);


					if (serializeFields != null)
						foreach (FieldInfo field in serializeFields)
							if (!field.Name.EndsWith("k__BackingField"))        // ignore property backing fields
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
					serializerMethod.parameters.Add(new ParameterDefinition("reader", typeof(NetIncomingMessage)));

					serializerClassDef.methods.Add(serializerMethod);

					FieldInfo[] serializeFields;
					PropertyInfo[] serializeProps;
					serializationAttr.GetSerializedMembers(paramType, out serializeFields, out serializeProps);


					if (serializeFields != null)
						foreach (FieldInfo field in serializeFields)
							if (!field.Name.EndsWith("k__BackingField"))        // ignore property backing fields
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