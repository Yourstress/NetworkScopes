
namespace NetworkScopes
{
	using System;
	using System.Reflection;
	using CodeGeneration;

	public class ScopeAttribute : InjectAttribute
	{
		readonly Type remoteType;

		public ScopeAttribute(Type remoteType)
		{
			this.remoteType = remoteType;
		}

		public override void ProcessClass(Type classType, ClassDefinition classDef)
		{
			// find remote methods
			MethodInfo[] remoteMethods = remoteType.GetMethods();

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
				// this is needed for IEnumerable<Peer>
				if (isServer)
					fakeRemoteClass.imports.Add("System.Collections.Generic");

				fakeRemoteClass.imports.Add("NetworkScopes");

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
			responseMethodDef.parameters.Add(new ParameterDefinition("packet", typeof(IncomingNetworkPacket)));

			Type retType = remoteMethod.ReturnType;

			retType = retType.GetGenericArguments()[0];

			int signalType = responseMethodDef.Name.GetConsistentHashCode();
			string optionalPeerParameter = isServer ? "SenderPeer, " : "";
			string deserializeCall = SerializationUtility.MakeReadCall("packet", retType);

			string dequeueInstruction =
				$"_Remote.remoteResponseTasks.DequeueResponse({optionalPeerParameter}{signalType}, {deserializeCall});";

			responseMethodDef.instructions.AddInstruction(dequeueInstruction);

			return responseMethodDef;
		}

		private MethodDefinition CreateReceiveMethod(MethodInfo localMethod)
		{
			MethodDefinition receiveMethodDef = new MethodDefinition("Receive_" + localMethod.Name);

			// only has one parameter: NetworkReader
			receiveMethodDef.parameters.Add(new ParameterDefinition("packet", typeof(IncomingNetworkPacket)));

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
					receiveMethodDef.Import(typeof(NetworkScopes.Signal));

					SignalResponseData signalResponseData = new SignalResponseData(localMethod);

					signalResponseData.ImportTypesInto(receiveMethodDef);

					if (signalResponseData.isTask)
						receiveMethodDef.IsAsync = true;

					int signalType = $"Response_{localMethod.Name}".GetConsistentHashCode();

					// call receiving method, and store its return value
					receiveMethodDef.instructions.AddInstruction($"{signalResponseData.ResponseTypeName} responseObject = {(signalResponseData.isTask ? "await " : "")}{localMethod.Name}({string.Join(", ", Array.ConvertAll(paramInfos, pi => pi.Name))});");
					receiveMethodDef.instructions.AddInstruction("");

					// create response packet
					receiveMethodDef.instructions.AddInstruction($"{typeof(OutgoingNetworkPacket).GetTypeName()} responsePacket = CreatePacket({signalType});");

					// serialize response object
					receiveMethodDef.instructions.AddInstruction(SerializationUtility.MakeWriteStatement("responseObject", "responsePacket", signalResponseData.responseType));

					// send out send the returned value
					receiveMethodDef.instructions.AddInstruction($"SendPacket(responsePacket);");
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
			fakeMethodDef.instructions.AddInstruction("OutgoingNetworkPacket packet = _netSender.CreatePacket({0});", remoteMethod.Name.GetConsistentHashCode());

			ParameterInfo[] paramInfos = remoteMethod.GetParameters();

			for (int x = 0; x < fakeMethodDef.parameters.Count; x++)
			{
				ParameterDefinition paramDef = fakeMethodDef.parameters[x];

				Type paramType = paramInfos[x].ParameterType;

				AddSerializeCode(fakeMethodDef, paramDef.Name, paramType);
			}

			// send it out
			fakeMethodDef.instructions.AddInstruction("_netSender.SendPacket(packet);");

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

		private void VerifySerializationExists(Type type)
		{
			if (!type.CanSerializeAtRuntime())
				NetworkDebug.LogWarning($"NetworkScopes can not serialize the type <color=orange>{type.Name}</color>. Consider implementing the <color=white>ISerializable</color> interface, or using the <color=white>[ProtoContract]</color> and <color=white>[ProtoMember]</color> attributes to serialize it using Protobuf.");
		}

		private bool AddSerializeCode(MethodDefinition method, string paramName, Type paramType)
		{
			method.Import(paramType);

			VerifySerializationExists(paramType);

			// write parameter
			method.instructions.AddInstruction(SerializationUtility.MakeWriteStatement(paramName, "packet", paramType));

			return true;
		}

		// when localMethod is null, it means serialization is happening within a custom-serialized class
		private bool AddDeserializeCode(MethodDefinition method, string paramName, Type paramType, MethodInfo localMethod)
		{
			method.Import(paramType);

			VerifySerializationExists(paramType);

			// read parameter
			method.instructions.AddInstruction(SerializationUtility.MakeReadAndAssignStatement(paramName, "packet", paramType));

			return true;
		}
	}
}