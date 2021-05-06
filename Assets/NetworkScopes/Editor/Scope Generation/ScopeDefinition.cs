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
	public class ScopeDefinition
	{
		public Type scopeInterface { get; private set; }

		public Type otherScopeInterface
		{
			get { return scopeAttribute.otherScopeType; }
		}

		public ScopeAttribute scopeAttribute { get; private set; }

		public ClassDefinition scopeDefinition { get; private set; }

		public SerializationProvider serializer { get; private set; }

		public EntityType entityType { get; private set; }
		public bool isServerScope { get { return entityType == EntityType.Server; } }

		public ScopeDefinition(Type scopeInterface, ScopeAttribute scopeAttribute, EntityType entityType, SerializationProvider serializer)
		{
			this.scopeInterface = scopeInterface;
			this.scopeAttribute = scopeAttribute;
			this.entityType = entityType;
			this.serializer = serializer;

			string name = scopeInterface.Name;
			scopeDefinition = new ClassDefinition(name.Substring(1, name.Length - 1));
			scopeDefinition.type.Namespace = scopeInterface.Namespace;

			// if the scope will have at least one abstract method, it must be abstract itself
			scopeDefinition.isAbstract = scopeAttribute.defaultReceiveType == SignalReceiveType.AbstractMethod;

			if (scopeDefinition.isAbstract)
				scopeDefinition.type.Name += "_Abstract";

			scopeDefinition.ResolveImportType(typeof(GeneratedAttribute));

			// generate sender class from the other scope's interface
			ClassDefinition senderInterface = GenerateScopeSender();

			// generate methods that send out the signals specified on the other scope's interface
			GenerateSenderMethods(senderInterface.type);

			// generate methods that receive signals sent by the other scope
			GenerateReceiverMethods();
		}

		private ClassDefinition GenerateScopeSender()
		{
			ClassDefinition senderInterface = new ClassDefinition("ISender");
			senderInterface.isInterface = true;
			senderInterface.baseType = typeof(IScopeSender);

			// add sender interface methods
			foreach (MethodInfo method in otherScopeInterface.GetMethods())
			{
				MethodDefinition methodDef = new MethodDefinition(method.Name, AccessModifier.Private);

				if (method.ReturnType != typeof(void))
					methodDef.ReturnType = SerializationProvider.GetPromiseType(method.ReturnType);

				methodDef.Body = null;
				methodDef.Parameters.AddRange(method.GetParameters().Select(p => new ParameterDefinition(p.Name, p.ParameterType)));
				senderInterface.methods.Add(methodDef);
			}

			// and finally, make the scope def class an implementor of this interface
			Type scopeConcreteBaseType = isServerScope ? typeof(ServerScope<>) : typeof(ClientScope<>);
			TypeDefinition nestedClassType =
				new TypeDefinition(string.Format("{0}.{1}", scopeDefinition.type.Name, senderInterface.type.Name));
			scopeDefinition.baseType = TypeDefinition.MakeGenericType(scopeConcreteBaseType, nestedClassType);
			scopeDefinition.interfaces.Add(nestedClassType);

			scopeDefinition.nestedClasses.Add(senderInterface);

			return senderInterface;
		}

		private void GenerateSenderMethods(TypeDefinition senderInterfaceType)
		{
			// generate interface implementation methods
			MethodDefinition method_GetScopeSender = new MethodDefinition("GetScopeSender", AccessModifier.Protected, MethodModifier.Override);
			method_GetScopeSender.ReturnType = senderInterfaceType;
			method_GetScopeSender.Body.AddReturnStatement("this");
			scopeDefinition.methods.Add(method_GetScopeSender);

			foreach (MethodInfo method in otherScopeInterface.GetMethods())
			{
				// sender method name
				MethodDefinition senderMethod = new MethodDefinition(method.Name, AccessModifier.Private);
				senderMethod.Name = "ISender." + senderMethod.Name;
				senderMethod.Parameters.AddRange(method.GetParameters().Select(p => new ParameterDefinition(p.Name, p.ParameterType)));

				bool isPromiseSender = method.ReturnType != typeof(void);

				// create signal command
				if (!isPromiseSender)
				{
					senderMethod.Body.AddAssignmentInstruction(typeof(ISignalWriter), "writer",
						string.Format("CreateSignal({0} /*hash '{1}'*/)", method.Name.GetConsistentHashCode(), method.Name));
				}
				// create promise command
				else
				{
					// allocate new promise before creating the signal
					TypeDefinition promiseType = SerializationProvider.GetPromiseType(method.ReturnType);

					// [PromiseType] promise = new [PromiseType]();
					senderMethod.Body.AddAssignmentInstruction(promiseType, "promise", string.Format("new {0}()", promiseType.Name));

					// then call CreatePromiseSignal
					senderMethod.Body.AddAssignmentInstruction(typeof(ISignalWriter),
						"writer",
						$"CreatePromiseSignal({method.Name.GetConsistentHashCode()}, promise /*hash '{method.Name}'*/)");

					// finally, make sure the method returns a promise type
					senderMethod.ReturnType = promiseType;
				}

				// write parameters one by one as received
				foreach (ParameterInfo param in method.GetParameters())
				{
					serializer.AddSerializationCommands(senderMethod.Body, param.Name, param.ParameterType);
				}

				// send signal command
				senderMethod.Body.AddLocalMethodCall("SendSignal", "writer");

				if (isPromiseSender)
				{
					senderMethod.Body.AddReturnStatement("promise");
				}

				scopeDefinition.methods.Add(senderMethod);
			}
		}

		private void GenerateReceiverMethods()
		{
			// find all methods defined in the scope's own interface
			MethodInfo[] methods = scopeInterface.GetMethods();
			MethodInfo[] promiseMethods = otherScopeInterface.GetMethods().Where(m => m.ReturnType != typeof(void)).ToArray();

			MethodModifier defaultReceiverModifier = (scopeAttribute.defaultReceiveType == SignalReceiveType.AbstractMethod)
				? MethodModifier.Abstract
				: MethodModifier.Virtual;

			bool isEventRaisingScope = scopeAttribute.defaultReceiveType == SignalReceiveType.Event;

			Type voidType = typeof(void);

			// write abstract receiver methods invoked by the raw receiver methods (to be written shortly after)
			foreach (MethodInfo method in methods)
			{
				MethodDefinition methodDef = new MethodDefinition(method.Name, AccessModifier.Protected, defaultReceiverModifier);

				bool mustSendResponse = method.ReturnType != voidType;
				// if the method has a return type, it must return one, and the result will be sent back to the other scope

				if (mustSendResponse)
				{
					methodDef.ReturnType = method.ReturnType;
					methodDef.Body.AddReturnStatement(string.Format("default({0})", method.ReturnType.GetReadableName()));
				}

				IEnumerable<ParameterDefinition> paramDefs = method.GetParameters().Select(p => new ParameterDefinition(p.Name, p.ParameterType));

				methodDef.Parameters.AddRange(paramDefs);
				scopeDefinition.methods.Add(methodDef);

				// create delegate for event type if needed
				if (isEventRaisingScope && !mustSendResponse)
				{
					// delegate definition
					DelegateDefinition delegateDef = new DelegateDefinition(method.Name+"Delegate", paramDefs);

					scopeDefinition.delegates.Add(delegateDef);

					// event declaration
					scopeDefinition.events.Add(new EventDefinition("On"+method.Name, delegateDef));
				}
			}

			// write receiver methods that take an ISignalReader param to read the signal's parameters, then invoke the abstract receiver method
			foreach (MethodInfo method in methods)
			{
				CreateReceiveMethod(method, isEventRaisingScope, false);
			}

			// write PROMISE RECEIVER methods that take an ISignalReader param - same as above
			foreach (MethodInfo promiseMethod in promiseMethods)
			{
				CreateReceiveMethod(promiseMethod, isEventRaisingScope, true);
			}
		}

		private void CreateReceiveMethod(MethodInfo method, bool isEventRaisingScope, bool isPromiseSignal)
		{
			bool mustSendResponse = method.ReturnType != typeof(void);

			string receiveMethodName = string.Format("{0}_{1}", (isPromiseSignal ? "ReceivePromise" : "ReceiveSignal"), method.Name);

			MethodDefinition receiver = new MethodDefinition(receiveMethodName, AccessModifier.Protected);
			receiver.Parameters.Add(new ParameterDefinition("reader", typeof(ISignalReader)));

			// forward promise ID so the other side can correlate promise with an object
//			if (mustSendResponse)
//				receiver.Body.AddMethodCall("writer", "WritePromiseIDFromReader", "reader");

			MethodBody body = receiver.Body;

			// use to store the parameters that make up each read command (per method)
			List<string> tempParamNames = new List<string>();

			foreach (ParameterInfo param in method.GetParameters())
			{
				serializer.AddDeserializationCommands(body, param.Name, param.ParameterType);
				tempParamNames.Add(param.Name);
			}

			// call receiving method
			string[] paramsStr = tempParamNames.ToArray();


			if (isEventRaisingScope && !mustSendResponse)
				body.AddLocalMethodCall("On"+method.Name, paramsStr);

			// promise receiver: notify the promise object that a value has arrived
			if (isPromiseSignal)
			{
				body.AddLocalMethodCall("ReceivePromise", "reader");
			}
			// promise sender: if method is defined with a non-void return type, it must send a value back
			else if (mustSendResponse)
			{
				body.AddMethodCallWithAssignment("promiseID", typeof(int).GetReadableName(), "reader", "ReadPromiseID");
				body.AddLocalMethodCallWithAssignment(method.Name, method.ReturnType.GetReadableName(), "promiseValue", paramsStr);

				string receiveHashName = "#" + method.Name;
				body.AddAssignmentInstruction(typeof(ISignalWriter), "writer",
					string.Format("CreateSignal({0} /*hash '{1}'*/)", receiveHashName.GetConsistentHashCode(), receiveHashName));

				serializer.AddSerializationCommands(body, "promiseID", typeof(int));
				serializer.AddSerializationCommands(body, "promiseValue", method.ReturnType);

				if (isServerScope)
					body.AddLocalMethodCall("SendSignal", "writer", "SenderPeer");
				else
					body.AddLocalMethodCall("SendSignal", "writer");
			}
			// regular signal: call local method
			else
				body.AddLocalMethodCall(method.Name, paramsStr);

			scopeDefinition.methods.Add(receiver);
		}

		public static string GetScopeRootPath(string scopeDefInterfaceName)
		{
			string interfacePath = FileUtility.FindInterfacePath(scopeDefInterfaceName);
			return Path.GetDirectoryName(interfacePath);
		}

		public static string MakeScopeScriptPath(string scopeName, string interfaceName)
		{
			string path = Path.Combine(GetScopeRootPath(interfaceName), "Generated");
			Directory.CreateDirectory(path);
			return Path.Combine(path, $"{scopeName}.cs");
		}

		public string GetScopeScriptPath()
		{
			return MakeScopeScriptPath(scopeDefinition.type.Name, scopeInterface.Name);
		}
	}
}