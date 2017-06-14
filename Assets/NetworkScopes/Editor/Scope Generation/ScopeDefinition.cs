using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NetworkScopes.CodeGeneration
{
	public class ScopeDefinition
	{
		public Type scopeInterface { get; private set; }

		public Type otherScopeInterface
		{
			get { return scopeAttribute.otherScopeType; }
		}

		public bool isServerScope { get; private set; }

		public ScopeAttribute scopeAttribute { get; private set; }

		public ClassDefinition scopeDefinition { get; private set; }

		public SerializationProvider serializer { get; private set; }

		public ScopeDefinition(Type scopeInterface, ScopeAttribute scopeAttribute, bool isServerScope, SerializationProvider serializer)
		{
			this.scopeInterface = scopeInterface;
			this.scopeAttribute = scopeAttribute;
			this.isServerScope = isServerScope;
			this.serializer = serializer;

			string name = scopeInterface.Name;
			scopeDefinition = new ClassDefinition(name.Substring(1, name.Length - 1));
			scopeDefinition.type.Namespace = scopeInterface.Namespace;

			// if the scope will have at least one abstract method, it must be abstract itself
			scopeDefinition.isAbstract = scopeAttribute.defaultReceiveType == SignalReceiveType.AbstractMethod;

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

				// create signal command
				senderMethod.Body.AddAssignmentInstruction(typeof(ISignalWriter), "writer",
					string.Format("CreateSignal({0})", senderMethod.Name.GetHashCode()));

				// write parameters one by one as received
				foreach (ParameterInfo param in method.GetParameters())
				{
					serializer.AddSerializationCommands(senderMethod.Body, param.Name, param.ParameterType);
				}

				// send signal command
				senderMethod.Body.AddLocalMethodCall("SendSignal", "writer");

				scopeDefinition.methods.Add(senderMethod);
			}
		}

		private void GenerateReceiverMethods()
		{
			// find all methods defined in the scope's own interface
			MethodInfo[] methods = scopeInterface.GetMethods();

			MethodModifier defaultReceiverModifier = (scopeAttribute.defaultReceiveType == SignalReceiveType.AbstractMethod)
				? MethodModifier.Abstract
				: MethodModifier.Virtual;

			bool isEventRaisingScope = scopeAttribute.defaultReceiveType == SignalReceiveType.Event;

			// write abstract receiver methods invoked by the raw receiver methods (to be written shortly after)
			foreach (MethodInfo method in methods)
			{
				MethodDefinition methodDef = new MethodDefinition(method.Name, AccessModifier.Protected, defaultReceiverModifier);

				IEnumerable<ParameterDefinition> paramDefs = method.GetParameters().Select(p => new ParameterDefinition(p.Name, p.ParameterType));

				methodDef.Parameters.AddRange(paramDefs);
				scopeDefinition.methods.Add(methodDef);

				// create delegate for event type if needed
				if (isEventRaisingScope)
				{
					// delegate definition
					DelegateDefinition delegateDef = new DelegateDefinition(method.Name+"Delegate", paramDefs);

					scopeDefinition.delegates.Add(delegateDef);

					// event declaration
					scopeDefinition.events.Add(new EventDefinition("On"+method.Name, delegateDef));
				}
			}

			// use to store the parameters that make up each read command (per method)
			List<string> tempParamNames = new List<string>();

			// write receiver methods that take an ISignalReader param to read the signal's parameters, then invoke the abstract receiver method
			foreach (MethodInfo method in methods)
			{
				MethodDefinition receiver = new MethodDefinition("Receive_" + method.Name, AccessModifier.Protected);
				receiver.Parameters.Add(new ParameterDefinition("reader", typeof(ISignalReader)));

				tempParamNames.Clear();
				foreach (ParameterInfo param in method.GetParameters())
				{
					serializer.AddDeserializationCommands(receiver.Body, param.Name, param.ParameterType);
					tempParamNames.Add(param.Name);
				}

				string[] paramsStr = tempParamNames.ToArray();

				if (isEventRaisingScope)
					receiver.Body.AddLocalMethodCall("On"+method.Name, paramsStr);
				receiver.Body.AddLocalMethodCall(method.Name, paramsStr);

				scopeDefinition.methods.Add(receiver);
			}
		}

		public string GetScopeScriptPath()
		{
			string[] guids = AssetDatabase.FindAssets(string.Format("t:MonoScript {0}", scopeDefinition.type.Name));

			if (guids.Length == 0)
				throw new Exception(
					string.Format("Could not find the file containing the type {0}. Please make sure the filename matches the interface name.", scopeDefinition.type.Name));

			string path = AssetDatabase.GUIDToAssetPath(guids[0]);

			path = Path.GetDirectoryName(path);
			return Path.Combine(path, string.Format("{0}.cs", scopeDefinition.type.Name));
		}
	}
}