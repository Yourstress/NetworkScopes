
//#define NETSCOPES_DEBUG_SHOW_GENERATED_SEND_METHODS
//#define NETSCOPES_DEBUG_SHOW_GENERATED_SEND_METHOD_INSTRUCTIONS

//#define NETSCOPES_DEBUG_SHOW_GENERATED_RECEIVE_METHODS

// Enable this to allow using DebugPreProcessedIL and DebugPostProcessedIL attributed.
#define DEBUG_IL

using System.Reflection.Emit;
using UnityEditor.Callbacks;
using System.Threading;
using Mono.Collections.Generic;
using System.Text;

namespace NetworkScopes.Editor
{
	using Mono.Cecil;
	using Mono.Cecil.Cil;
	using System;
	using System.IO;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Linq;
	using System.Reflection;

	[InitializeOnLoad]
	public static class NetworkScopeAssemblyPostProcessor
	{
		static Thread fileWatcherThread = null;

		static void WatchAssemblyThenPatch()
		{
			if (fileWatcherThread == null)
			{
				string appPath = EditorApplication.applicationPath;
				string assemPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Temp/StagingArea/Data/Managed/");
				
				// combine PATHDIR/Temp/..
				fileWatcherThread = new Thread(() =>
					{
						string filePath = assemPath + "Assembly-CSharp.dll";

						while (true)
						{
							if (File.Exists(filePath))
							{
								using (AssemblyExplorer assem = AssemblyExplorer.FromDirectory(appPath, assemPath))
								{
									PatchScopes(assem);
								}

								break;
							}
							
							Thread.Sleep(10);
						}

						fileWatcherThread = null;
					});

				fileWatcherThread.Start();
			}
		}

		[PostProcessScene]
		public static void OnPostprocessBuild ()
		{
			WatchAssemblyThenPatch();
		}
		static NetworkScopeAssemblyPostProcessor ()
		{
			using (AssemblyExplorer assem = AssemblyExplorer.FromLoadedAssemblies())
			{
				PatchScopes(assem);
			}
		}

		static void PatchScopes(AssemblyExplorer assem)
		{
			try
			{
				#if DEBUG_IL
				// show preprocessed IL
				foreach (var method in assem.FindMethodsWithAttribute(typeof(DebugPreProcessedIL)))
				{
					Debug.LogFormat("<color=gray>{0}.{1}</color>", method.Key.DeclaringType.Name, method.Key.Name);
					foreach (var instruction in method.Key.Body.Instructions)
					{
						Debug.LogFormat("<color=gray>{0}</color>", instruction);
					}
				}
				#endif

				PatchScopeTypes(assem, typeof(ServerScope<,>), "Client");
				PatchScopeTypes(assem, typeof(ClientScope<,>), "Server");

				assem.SaveChangedAssemblies();

				#if DEBUG_IL
				// show postprocessed IL
				foreach (var method in assem.FindMethodsWithAttribute(typeof(DebugPostProcessedIL)))
				{
				Debug.LogFormat("<color=yellow>{0}.{1}</color>", method.Key.DeclaringType.Name, method.Key.Name);
				foreach (var instruction in method.Key.Body.Instructions)
				{
				Debug.LogFormat("<color=yellow>{0}</color>", instruction);
				}
				}
				#endif
			}
			catch (Exception e)
			{
				Debug.LogWarning (e);
			}
		}

		private static void PatchScopeTypes(AssemblyExplorer assem, Type baseScopeClass, string remoteEndpointFieldName)
		{
//			DateTime dt = DateTime.Now;
			foreach (TypeDefinition scopeTypeDefinition in assem.FindSubclassTypes(baseScopeClass))
			{
//				Debug.Log(scopeKvp.Value.ConstructorArguments[0].Value);

//				Debug.Log("Processing scope of type " + scopeTypeDefinition.Name + " in assembly " + scopeTypeDefinition.Module.Assembly.FullName);

				#if DEBUG_SCOPE_PATCHES
				Debug.Log ("<color=blue> PROCESSING </color> " + localTypeDefinition);
				#endif

				// the scope's base class should match localEndpointClassType
				GenericInstanceType localGenericBaseTypeRef = (GenericInstanceType)scopeTypeDefinition.BaseType;
				TypeDefinition localBaseTypeDef = scopeTypeDefinition.BaseType.Resolve();
				if (localBaseTypeDef.FullName != baseScopeClass.FullName)
				{
					assem.MarkAssemblyChanged(scopeTypeDefinition.Module.Assembly);
					Debug.LogWarningFormat("Could not patch scope '{0}' because it does not inherit from {1}", scopeTypeDefinition, baseScopeClass);
					continue;
				}

				// get the client field
				FieldReference remoteFieldRef = localBaseTypeDef.Fields.FirstOrDefault(f => f.Name == remoteEndpointFieldName).MakeGeneric(localGenericBaseTypeRef.GenericArguments.ToArray());

				if (remoteFieldRef == null) 
				{
					assem.MarkAssemblyChanged(scopeTypeDefinition.Module.Assembly);
					Debug.LogWarningFormat("Could not patch scope '{0}' because it doesn't contain a remote endpoint field", scopeTypeDefinition);
					continue;
				}

				// replace any client type calls to underlying client scope - methods turned to array because CreateDeserializeMethod is creating methods
				foreach (MethodDefinition scopeMethodDefinition in scopeTypeDefinition.Methods.ToArray()) 
				{
					// patch any calls to the remote scope
					PatchScopeMethod(scopeTypeDefinition, scopeMethodDefinition, remoteFieldRef);

					// create deserialize method for each public method in this class (called by the other endpoint)
					if (scopeMethodDefinition.HasThis && !scopeMethodDefinition.IsConstructor && !scopeMethodDefinition.IsSpecialName &&
						scopeMethodDefinition.IsPublic && scopeMethodDefinition.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(Signal).FullName)) 
					{
						CreateUnderlyingReceiveMethod(scopeTypeDefinition, scopeMethodDefinition.Name, scopeMethodDefinition, null);
					}
				}

				// also, create receive methods for Event fields
				foreach (FieldDefinition eventField in scopeTypeDefinition.Fields.Where(f => f.IsPublic && NetworkEventUtility.IsEventType(f.FieldType.Name)))
				{
					GenericInstanceType geninst = (GenericInstanceType)eventField.FieldType;

					MethodReference invokeMethod = eventField.FieldType.Resolve().Methods.First(m => m.Name == "Invoke");

					try
					{
						MethodReference mref = new MethodReference(invokeMethod.Name, invokeMethod.ReturnType, geninst)
						{
							HasThis = invokeMethod.HasThis,
							ExplicitThis = invokeMethod.ExplicitThis,
							CallingConvention = invokeMethod.CallingConvention,
						};

						foreach (TypeReference param in geninst.GenericArguments) 
						{
							ParameterDefinition pd = new ParameterDefinition(param.Name, Mono.Cecil.ParameterAttributes.None, param);
							mref.Parameters.Add(pd);
						}

						invokeMethod = mref;
					}
					catch (Exception e)
					{
						Debug.Log("Could not " + eventField.Name);
						Debug.Log(e);
					}


//					Debug.Log(eventField.FieldType.GenericArguments.Count);
//					Debug.Log(eventField.FieldType.generic.Count);

//					if (eventField.generi.IsGenericInstance)
//					{
//						Debug.Log("===" + invokeMethod.MakeGeneric(geninst.GenericArguments.ToArray()));
//					}

					CreateUnderlyingReceiveMethod(scopeTypeDefinition, eventField.Name, invokeMethod, eventField);
				}

				// patch all other types referencing remoteFieldRef within the same assembly
				foreach (ModuleDefinition module in scopeTypeDefinition.Module.Assembly.Modules)
				{
					foreach (TypeDefinition type in module.Types)
					{
						// skip localtype
						if (type == scopeTypeDefinition)
							continue; 
						
						foreach (MethodDefinition method in type.Methods)
						{
							if (method.IsAbstract || !method.HasBody)
								continue;

							PatchScopeMethod(scopeTypeDefinition, method, remoteFieldRef);
						}
					}
				}

				assem.MarkAssemblyChanged(scopeTypeDefinition.Module.Assembly);

				#if DEBUG_SCOPE_PATCHES
				Debug.Log ("<color=green>Patched server</color>");
				#endif
			}
		}

		private static void PatchScopeMethod(TypeDefinition localEndpointTypeDefinition, MethodDefinition methodDefinition, FieldReference remoteFieldRef)
		{
			// find a call to the client field
			bool didFindRemoteField = false;
			int lastRemoteFieldIndex = -1;
			int lastOtherFieldIndex = -1;

			// find base
			GenericInstanceType genericBase = (GenericInstanceType)localEndpointTypeDefinition.BaseType;
			TypeReference[] genericArgs = genericBase.GenericArguments.ToArray();

			CustomAttribute showILAttr = methodDefinition.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == typeof(DebugIL).FullName);

			// remove so we don't re-process
//			methodDefinition.CustomAttributes.Remove(showILAttr);

			bool showIL = showILAttr != null;

			if (showIL)
				Debug.LogFormat("<color=yello>{0}.{1} Instructions</color>", methodDefinition.DeclaringType.Name, methodDefinition.Name);

			for (int x = 0; x < methodDefinition.Body.Instructions.Count; x++)
			{
				Instruction inst = methodDefinition.Body.Instructions[x];

				Code code = inst.OpCode.Code;

				// find a load field instruction that's loading a field matching the specified client field
				if (code == Code.Ldfld)
				{
					FieldReference fld = (FieldReference)inst.Operand;

					// make sure it's the same -- the references might not be equal, but the full name will suffice for checking equality
					if (fld.Name == remoteFieldRef.Name &&
						fld.Resolve().MakeGeneric(genericArgs).DeclaringType.FullName == remoteFieldRef.DeclaringType.FullName)
					{
						if (showIL)
							Debug.LogFormat("<color=yellow>{0}</color>", inst);
						
						// remove the instruction
						didFindRemoteField = true;
						lastRemoteFieldIndex = x;

						continue;
					}
					else
						lastOtherFieldIndex = x;
				}
				// find a method call to the previously deleted field's method
				else if (code == Code.Callvirt && didFindRemoteField)
				{
					MethodDefinition callMethod = ((MethodReference)inst.Operand).Resolve();
					var callAttributes = callMethod.CustomAttributes;

					string callMethodName;

					// must be calling the remote endpoint type
					if (callMethod.DeclaringType != genericArgs[1])
					{
						// see if it's an event delegate type
						if (NetworkEventUtility.IsEventType(callMethod.DeclaringType.Name))
						{
							FieldReference delegateField = (FieldReference)methodDefinition.Body.Instructions[lastOtherFieldIndex].Operand;

							callAttributes = delegateField.Resolve().CustomAttributes;
							callMethodName = delegateField.Name;

							if (delegateField.FieldType.IsGenericInstance)
							{
								GenericInstanceType genInst = (GenericInstanceType)delegateField.FieldType;

								callMethod.Parameters.Clear();

								for (int gp = 0; gp < genInst.GenericArguments.Count; gp++)
								{
									ParameterDefinition pd = new ParameterDefinition(genInst.GenericArguments[gp].GetElementType());
									callMethod.Parameters.Add(pd);
								}
							}

							// remove the loading of the other field
							methodDefinition.Body.Instructions.RemoveAt(lastOtherFieldIndex);

							if (lastOtherFieldIndex < lastRemoteFieldIndex)
								lastRemoteFieldIndex++;

							x--;
								
							lastOtherFieldIndex = -1;
						}
						else
							continue;
					}
					else
					{
						callMethodName = callMethod.Name;
					}
					
					// if the called method does not have the Signal attribute, don't patch it
					if (!callAttributes.Any(attr => attr.AttributeType.FullName == typeof(Signal).FullName))
					{
						Debug.LogWarningFormat("Can not call send a Signal within the method <color=gray>{2}.{3}</color> because the target Scope method <color=gray>{0}.{1}</color> does not have a [Signal] attribute.", methodDefinition.DeclaringType.Name, methodDefinition.Name, localEndpointTypeDefinition.Name, callMethod.Name);
						continue;
					}

					if (showIL)
					{
						Debug.LogFormat("<color=yellow>Field call detected</color>", inst);
						Debug.LogFormat("<color=red>{0}</color>", inst);
					}
					
					MethodReference underlyingSendMethodDef = GetOrCreateUnderlyingSendMethod(localEndpointTypeDefinition, callMethodName, callMethod.Resolve());

					inst = Instruction.Create(OpCodes.Call, underlyingSendMethodDef);
					methodDefinition.Body.Instructions.RemoveAt(x);
					methodDefinition.Body.Instructions.Insert(x, inst);

					// delete the remote field
					methodDefinition.Body.Instructions.RemoveAt(lastRemoteFieldIndex);

					// make sure to re-iterate over the last index since we removed a field load
					x--;

					didFindRemoteField = false;
				}

				if (showIL)
					Debug.LogFormat("<color=yello>{0}</color>", inst);
			}

			if (showIL)
			{
				Debug.Log("");
			}
		}

		private static MethodReference GetOrCreateUnderlyingSendMethod(TypeDefinition localScopeType, string callMethodName, MethodDefinition remoteMethodDefinition)
		{
			string underlyingSendMethodName = string.Format("Send_{0}", callMethodName);

//			// if the remote method does not have the Signal attribute, generate a warning
//			if (!remoteMethodDefinition.CustomAttributes.Any(attr => attr.AttributeType.FullName == typeof(Signal).FullName))
//			{
//				Debug.LogWarningFormat("Could not generate an underlying send method for {0}.{1}", localScopeType.Name, remoteMethodDefinition);
//				return null;
//			}

			GenericInstanceType genericBase = (GenericInstanceType)localScopeType.BaseType;

			MethodDefinition methodDef = localScopeType.Methods.FirstOrDefault(m =>
				{
					return m.Name == underlyingSendMethodName &&
						m.Parameters.Count == remoteMethodDefinition.Parameters.Count;
				});

			MethodReference methodRef = null;

			// if method doesn't exist yet, create it
			if (methodDef == null)
			{
				#if NETSCOPES_DEBUG_SHOW_GENERATED_SEND_METHODS
				Debug.LogFormat("<color=green>Creating SEND method {0}.{1}</color>", localScopeType.Name, underlyingSendMethodName);
				#endif

				methodDef = new MethodDefinition(underlyingSendMethodName, remoteMethodDefinition.Resolve().Attributes, remoteMethodDefinition.ReturnType);

				foreach (ParameterDefinition parameter in remoteMethodDefinition.Parameters)
				{
					ParameterDefinition newParameter = new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType);
					methodDef.Parameters.Add(newParameter);
				}

				#if NETSCOPES_DEBUG_SHOW_SEND_METHOD_PATCHES
				Debug.Log("<color=cyan>CREATING "+ methodDef.FullName + "</color>");
				#endif

				// prepare method references
				TypeReference netWriterTypeRef = localScopeType.Module.Import(typeof(UnityEngine.Networking.NetworkWriter));
				TypeDefinition netWriterTypeDefinition = netWriterTypeRef.Resolve();

				MethodReference createWriterMethod = localScopeType.BaseType.Resolve().Methods.FirstOrDefault(m => m.Name == "CreateWriter").MakeGeneric(genericBase.GenericArguments.ToArray());
				MethodReference prepareAndSendMethod = localScopeType.BaseType.Resolve().Methods.FirstOrDefault(m => m.Name == "PrepareAndSendWriter").MakeGeneric(genericBase.GenericArguments.ToArray());

				createWriterMethod = localScopeType.Module.Import(createWriterMethod);
				prepareAndSendMethod = localScopeType.Module.Import(prepareAndSendMethod);

				// add net writer variable to the method body
				VariableDefinition netWriterVar = new VariableDefinition(netWriterTypeRef);
				methodDef.Body.Variables.Add(netWriterVar);

				ILProcessor il = methodDef.Body.GetILProcessor();

				// get "self"
				il.Emit(OpCodes.Ldarg_0);

				// load msgType to pass on to CreateWriter(short msgType)
				il.Emit(OpCodes.Ldc_I4, callMethodName.GetHashCode());

				// call CreateWriter
				il.Emit(OpCodes.Call, createWriterMethod);

				// ...and store it (the newly created NetworkWriter) in the first variable (netWriterVar)
				il.Emit(OpCodes.Stloc_0);
				
				// feed parameter serialization instructions as defined by the method parameters
				foreach (ParameterDefinition parameter in methodDef.Parameters)
				{
					TypeReference elementType = parameter.ParameterType;

					if (elementType.IsArray)
						elementType = elementType.GetElementType();

					// find a NetworkWriter Write method that can write the given parameter type
					MethodDefinition writeMethodDefinition = netWriterTypeDefinition.Methods.FirstOrDefault(m =>
						{
							return m.Name == "Write" &&
								m.Parameters.Count == 1 &&
								m.Parameters[0].ParameterType.FullName == elementType.FullName;
						});

					MethodDefinition customTypeSerializeMethod = null;
					
					if (writeMethodDefinition == null)
					{
						// try to read underlying enum type
						FieldDefinition underlyingEnumField = elementType.Resolve().Fields.FirstOrDefault(f => f.Name == "value__");

						// if it's an enum, serialize it as its underlying type
						if (underlyingEnumField != null)
						{
							writeMethodDefinition = netWriterTypeDefinition.Methods.FirstOrDefault(m =>
								m.Name == "Write" &&
								m.Parameters.Count == 1 &&
								m.Parameters[0].ParameterType.FullName == underlyingEnumField.FieldType.FullName);
						}
						// otherwise attempt to serialize it as a custom type (look for the NetworkSerialization attribute)
						else
						{
							TypeDefinition customTypeDef = parameter.ParameterType.Resolve();

							// look for the attribute
							CustomAttribute netSerializationAttr = customTypeDef.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == typeof(NetworkSerialization).FullName);

							// if not found, immediately show and error and move on
							if (netSerializationAttr == null)
							{
								Debug.LogErrorFormat("NetworkScopes: Failed to create Send method for <color=gray>{0}.{1}</color> because parameter of type <color=gray>{2}</color> could not be serialized. Use the NetworkSerialization attribute to enable serialization of custom classes.", remoteMethodDefinition.DeclaringType.Name, remoteMethodDefinition.Name, parameter.ParameterType.Name);
								continue;
							}

							customTypeSerializeMethod = SerializationILGenerator.GetOrCreateCustomTypeSerializeMethod(customTypeDef);
						}
					}
					

					// serialize all array elements in a loop if it's an array
					if (parameter.ParameterType.IsArray)
					{
						// add X (loop) variable
						il.Body.Variables.Add( new VariableDefinition(localScopeType.Module.Import(typeof(int))) );

						il.Emit(OpCodes.Ldloc_0);

						// load the array-typed parameter
						il.Emit(OpCodes.Ldarg_S, parameter);

						// get the array length
						il.Emit(OpCodes.Ldlen);

						il.Emit(OpCodes.Conv_I4);

						MethodReference writeIntMethodRef = netWriterTypeDefinition.Methods.FirstOrDefault(m =>
							{
								return m.Name == "Write" &&
									m.Parameters.Count == 1 &&
									m.Parameters[0].ParameterType.FullName == typeof(int).FullName;
							});
						
						// write the length
						il.Emit(OpCodes.Callvirt, localScopeType.Module.Import(writeIntMethodRef));

						// int x = 0;
						il.Emit(OpCodes.Ldc_I4_0);
						il.Emit(OpCodes.Stloc_1);

						// prepare for loop vars
						Instruction loopJump = il.Create(OpCodes.Ldloc_1);

						il.Emit(OpCodes.Br, loopJump);

						Instruction firstLoopInst;

						// check if custom deserializer is set, and use it
						if (customTypeSerializeMethod != null)
						{
							// load array arg and append as the first array instruction
							firstLoopInst = il.Create(OpCodes.Ldarg_1);
							il.Append(firstLoopInst);

							// load counter var
							il.Emit(OpCodes.Ldloc_1);

							// load array element
							if (elementType.IsValueType)
								il.Emit(OpCodes.Ldelem_I4);
							else
								il.Emit(OpCodes.Ldelem_Ref);

							// load NetworkWriter var
							il.Emit(OpCodes.Ldloc_0);

							// call custom type's static serialization method
							il.Emit(OpCodes.Call, localScopeType.Module.Import(customTypeSerializeMethod));
						}
						// otherwise use the NetworkWriter.Write method with the corresponding parameter type
						else
						{
							// load NetworkWriter arg and append as the first array instruction
							firstLoopInst = il.Create(OpCodes.Ldloc_0);
							il.Append(firstLoopInst);

							il.Emit(OpCodes.Ldarg_S, parameter);
							il.Emit(OpCodes.Ldloc_1);

							// load array element
							if (elementType.IsValueType)
								il.Emit(OpCodes.Ldelem_I4);
							else
								il.Emit(OpCodes.Ldelem_Ref);
							
							MethodReference writeMethodRef = localScopeType.Module.Import(writeMethodDefinition);
							
							// make a call to write array element
							il.Emit(OpCodes.Callvirt, writeMethodRef);
						}

						il.Emit(OpCodes.Ldloc_1);
						il.Emit(OpCodes.Ldc_I4_1);
						il.Emit(OpCodes.Add);
						il.Emit(OpCodes.Stloc_1);
						il.Append(loopJump);
						il.Emit(OpCodes.Ldarg_S, parameter);
						il.Emit(OpCodes.Ldlen);
						il.Emit(OpCodes.Conv_I4);
						il.Emit(OpCodes.Blt, firstLoopInst);
					}
					// or just serialize once
					else
					{
						// check if custom serializer is set, and use it
						if (customTypeSerializeMethod != null)
						{
							// load the custom type
							il.Emit(OpCodes.Ldarg_S, parameter);

							// load the network writer
							il.Emit(OpCodes.Ldloc_0);

							// call custom type's static serialization method
							il.Emit(OpCodes.Call, localScopeType.Module.Import(customTypeSerializeMethod));
						}
						// otherwise serialize using the specified write method
						else
						{
							il.Emit(OpCodes.Ldloc_0);
							il.Emit(OpCodes.Ldarg_S, parameter);
							il.Emit(OpCodes.Callvirt, localScopeType.Module.Import(writeMethodDefinition));
						}
					}
				}

				il.Emit(OpCodes.Ldarg_0);

				// load the newly created netWriterCtor variable from vars[0]
				il.Emit(OpCodes.Ldloc_0);

				// call base.PrepareAndSendWriter
				il.Emit(OpCodes.Call, prepareAndSendMethod);

				// and finally, the return statement
				il.Emit(OpCodes.Ret);

				// add method to the base type
				localScopeType.Methods.Add(methodDef);

				#if NETSCOPES_DEBUG_SHOW_GENERATED_SEND_METHOD_INSTRUCTIONS
				foreach (Instruction inst in il.Body.Instructions)
				{
					Debug.Log("<color=cyan>"+ inst + "</color>");
				}
				#endif
			}
			else
			{
//				Debug.Log("MEthod already defined linked in " + localScopeType.Name + "." + remoteMethodDefinition.Name);
			}

			// resolve method reference
			methodRef = localScopeType.Module.Import(methodDef);

			return methodRef;
		}

		private static void CreateUnderlyingReceiveMethod(TypeDefinition localScopeTypeDef, string calledMethodName, MethodReference invokeMethod, FieldDefinition customEventField)
		{
			string receiveMethodName = string.Format("Receive_{0}", calledMethodName);

			ModuleDefinition module = localScopeTypeDef.Module;

			// if the method already exists, terminate right here
			if (localScopeTypeDef.Methods.Any(m => m.Name == receiveMethodName))
				return;

			// create method
			MethodDefinition recvMethod = new MethodDefinition(receiveMethodName, Mono.Cecil.MethodAttributes.Private, localScopeTypeDef.Module.Import(typeof(void)));

			// find serializer function
			TypeDefinition netReaderTypeDefinition = localScopeTypeDef.Module.Import(typeof(NetworkReader)).Resolve();

			// add NetworkReader as a method input parameter
			ParameterDefinition netReaderParamDefinition = new ParameterDefinition(localScopeTypeDef.Module.Import(netReaderTypeDefinition));
			recvMethod.Parameters.Add(netReaderParamDefinition);

			List<VariableDefinition> callMethodParameterVars = new List<VariableDefinition>();

			ILProcessor il = recvMethod.Body.GetILProcessor();

			// find netreader deserializer function for every parameter
			foreach (ParameterDefinition param in invokeMethod.Parameters)
			{
				// create variable and add it to the body
				VariableDefinition variable = new VariableDefinition(param.ParameterType);
				il.Body.Variables.Add(variable);

				// add it to the list of just the method params, without any extra body vars created (loop var etc)
				callMethodParameterVars.Add(variable);

				TypeReference paramType = param.ParameterType;

				if (paramType.IsArray)
					paramType = paramType.GetElementType();

				MethodReference readMethodRef;

				// find deserializer type for the param
				readMethodRef = netReaderTypeDefinition.Methods.FirstOrDefault(m =>
					m.Name.StartsWith("Read") &&
					m.ReturnType.FullName == paramType.FullName);

				// if failed, try other methods to serialize the field type
				if (readMethodRef == null)
				{
					// try to read underlying enum type
					FieldDefinition underlyingEnumField = paramType.Resolve().Fields.FirstOrDefault(f => f.Name == "value__");

					if (underlyingEnumField != null)
					{
						readMethodRef = netReaderTypeDefinition.Methods.FirstOrDefault(m =>
							m.Name.StartsWith("Read") &&
							m.ReturnType.FullName == underlyingEnumField.FieldType.FullName);
					}
				}

				MethodDefinition customTypeDeserializeMethod = null;

				if (readMethodRef == null)
				{
					TypeDefinition customTypeDef = param.ParameterType.Resolve();

					// look for the attribute
					CustomAttribute netSerializationAttr = customTypeDef.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == typeof(NetworkSerialization).FullName);

					// if not found, immediately show and error and move on
					if (netSerializationAttr == null)
					{
						Debug.LogErrorFormat("NetworkScopes: Failed to create Receive method for <color=gray>{0}.{1}</color> because parameter of type <color=gray>{2}</color> could not be serialized. Use the NetworkSerialization attribute to enable serialization of custom classes", localScopeTypeDef.Name, invokeMethod.Name, param.ParameterType.Name);
						continue;
					}

					// create static NetworkDeserialize method
					customTypeDeserializeMethod = SerializationILGenerator.GetOrCreateCustomTypeDeserializeMethod(customTypeDef);
				}

				if (param.ParameterType.IsArray)
				{
					// get the NetworkReader.ReadInt32() method to read the array length
					MethodReference readIntMethodRef = netReaderTypeDefinition.Methods.FirstOrDefault(m =>
						{
							return m.Name.StartsWith("Read") &&
								m.ReturnType.FullName == typeof(int).FullName;
						});

					var intTypeRef = module.Import(typeof(int));

					// add necessary variables
					VariableDefinition arrayCounterVar = new VariableDefinition(intTypeRef);
					il.Body.Variables.Add(arrayCounterVar);

					VariableDefinition arrayLenVar = new VariableDefinition(intTypeRef);
					il.Body.Variables.Add(arrayLenVar);

					// load NetworkReader
					il.Emit(OpCodes.Ldarg_1);

					// call NetworkReader.ReadInt32()
					il.Emit(OpCodes.Callvirt, module.Import(readIntMethodRef));

					// store value in variable (len)
					il.Emit(OpCodes.Stloc_S, arrayLenVar);

					// load it up for the next instruction
					il.Emit(OpCodes.Ldloc_S, arrayLenVar);

					// create a new array matching the element type
					il.Emit(OpCodes.Newarr, module.Import(paramType));

					// store it in our variable
					il.Emit(OpCodes.Stloc_S, variable);

					// int x = 0;
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Stloc_S, arrayCounterVar);

					// prepare for loop vars
					Instruction loopJump = il.Create(OpCodes.Ldloc_S, arrayCounterVar);

					il.Emit(OpCodes.Br, loopJump);

					Instruction firstLoopInst = il.Create(OpCodes.Ldloc_S, variable);
					il.Append(firstLoopInst);

					il.Emit(OpCodes.Ldloc_S, arrayCounterVar);

					// check if custom deserializer is set, and use it
					if (customTypeDeserializeMethod != null)
					{
						var customTypeCtor = paramType.Resolve().Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 0);

						// create new object of the custom type
						il.Emit(OpCodes.Newobj, module.Import(customTypeCtor));
					}
					// otherwise, just deserialize using NetworkReader.ReadX
					else
					{
						// load NetworkReader
						il.Emit(OpCodes.Ldarg_1);

						// call ReadX based on the element type
						il.Emit(OpCodes.Callvirt, module.Import(readMethodRef));
					}

					// store it into array element
					if (paramType.IsValueType)
						il.Emit(OpCodes.Stelem_I4);
					else
						il.Emit(OpCodes.Stelem_Ref);

					// if deserializer is set, call it with the array element at the loop counter index
					if (customTypeDeserializeMethod != null)
					{
						// load the array
						il.Emit(OpCodes.Ldloc_S, variable);

						// load counter index
						il.Emit(OpCodes.Ldloc_S, arrayCounterVar);

						// get the array element
						il.Emit(OpCodes.Ldelem_Ref);


						// load NetworkReader
						il.Emit(OpCodes.Ldarg_1);

						// call CustomType.NetworkDeserialize(CustomType,NetworkReader)
						il.Emit(OpCodes.Call, module.Import(customTypeDeserializeMethod));
					}

					il.Emit(OpCodes.Ldloc_S, arrayCounterVar);
					il.Emit(OpCodes.Ldc_I4_1);
					il.Emit(OpCodes.Add);
					il.Emit(OpCodes.Stloc_S, arrayCounterVar);
					il.Append(loopJump);

					il.Emit(OpCodes.Ldloc_S, arrayLenVar);
					il.Emit(OpCodes.Blt, firstLoopInst);
				}
				else
				{
					// check if custom deserializer is set, and use it
					if (customTypeDeserializeMethod != null)
					{
						var customTypeCtor = paramType.Resolve().Methods.FirstOrDefault(m => m.IsConstructor && m.Parameters.Count == 0);

						if (customTypeCtor == null)
						{
							Debug.LogErrorFormat("The type {0} can not be deserialized at runtime because it does not have a parameterless constructor", paramType.Name);
							return;
						}

						// create new object of the custom type
						il.Emit(OpCodes.Newobj, module.Import(customTypeCtor));

						// store value in variable
						il.Emit(OpCodes.Stloc_S, variable);

						// load it back up
						il.Emit(OpCodes.Ldloc_S, variable);

						// load NetworkReader
						il.Emit(OpCodes.Ldarg_1);

						// call CustomType.NetworkDeserialize(CustomType,NetworkReader)
						il.Emit(OpCodes.Call, module.Import(customTypeDeserializeMethod));
					}
					// otherwise serialize using NetworkReader's read method
					else
					{
						// load NetworkReader
						il.Emit(OpCodes.Ldarg_1);

						// call NetworkReader.ReadX
						il.Emit(OpCodes.Callvirt, module.Import(readMethodRef));

						// store value in variable
						il.Emit(OpCodes.Stloc_S, variable);
					}
				}
			}


			il.Emit(OpCodes.Ldarg_0);

			if (customEventField != null)
				il.Emit(OpCodes.Ldfld, customEventField);

			// load variables
			foreach (VariableDefinition variable in callMethodParameterVars)
			{
				il.Emit(OpCodes.Ldloc_S, variable);
			}

			OpCode callOp;

			// load the field if specified
			if (customEventField != null)
			{
				callOp = OpCodes.Callvirt;

				// Needful?
				invokeMethod.DeclaringType = customEventField.FieldType;
			}
			else
				callOp = OpCodes.Call;

			// call the method
			il.Emit(callOp, invokeMethod);

			il.Emit(OpCodes.Ret);

			#if NETSCOPES_DEBUG_SHOW_GENERATED_RECEIVE_METHODS
			StringBuilder sb = new StringBuilder(string.Format("<color=cyan>Creating RECEIVE method {0}.{1}</color>", localScopeTypeDef.Name, receiveMethodName));
			for (int debInst = 0; debInst < il.Body.Instructions.Count; debInst++)
			{
				sb.AppendFormat("\n<color=white>{0}</color>", il.Body.Instructions[debInst]);
			}
			Debug.Log(sb.ToString());
			#endif

			// add it to the method's owner class
			localScopeTypeDef.Methods.Add(recvMethod);
		}
	}
}
