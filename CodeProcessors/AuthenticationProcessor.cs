using System;
using CodeGeneration;
using Lidgren.Network;
using UnityEngine;

namespace NetworkScopes
{
}

namespace NetworkScopes.CodeProcessors
{
	public static class AuthenticationProcessor
	{
		public static void ProcessClient(Type t)
		{
			Authenticator authAttr = GetClientAuthenticatorAttribute(t);

			if (authAttr == null)
				return;

			IVariableSerialization serialization = GetAuthVariableSerialization(GetClientAuthenticationObjectType(authAttr), t);

			if (serialization == null)
				return;

			GenerateSerialization(serialization, t, false);
		}

		private static void GenerateSerialization(IVariableSerialization serialization, Type clientOrServerType, bool isServer)
		{
			// create partial class
			ClassDefinition partialClass = new ClassDefinition(clientOrServerType.Name, clientOrServerType.Namespace, true);
			partialClass.FileName = "G-" + clientOrServerType.Name;

			if (isServer)
			{
				/*
		protected override bool AuthenticatePeer(Peer peer, NetIncomingMessage msg)
		{
			Player authentication = new Player();
			try
			{
				Player.NetworkDeserialize(authentication, msg);
				return Authenticate(peer, authentication);
			}
			catch
			{
				return false;
			}
		}
				 */

				// create auth override method
				MethodDefinition authMethod = new MethodDefinition("AuthenticatePeer");
				authMethod.ReturnType = "bool";
				authMethod.accessModifier = MethodAccessModifier.Protected;
				authMethod.modifier = MethodModifier.Override;

				Type peerType = clientOrServerType.BaseType.GetGenericArguments()[0];
				authMethod.parameters.Add(new ParameterDefinition("peer", peerType.Name));
				authMethod.parameters.Add(new ParameterDefinition("msg", nameof(NetIncomingMessage)));

				authMethod.instructions.AddInstruction(string.Format("{0} auth = new {0}();", serialization.VariableType.Name));
				authMethod.instructions.AddInstruction("try");
				authMethod.instructions.AddInstruction("{");
//				authMethod.instructions.AddInstruction("\t"+serialization.CreateNetworkDeserializeCall("auth", "msg"));
				authMethod.instructions.AddInstruction("\t"+serialization.CreateNetworkDeserializeCall("msg"));
				authMethod.instructions.AddInstruction($"\treturn Authenticate(peer, auth);");
				authMethod.instructions.AddInstruction("}");
				authMethod.instructions.AddInstruction("catch");
				authMethod.instructions.AddInstruction("{");
				authMethod.instructions.AddInstruction("\treturn false;");
				authMethod.instructions.AddInstruction("}");

				partialClass.methods.Add(authMethod);

				partialClass.Import(peerType);
				partialClass.Import(typeof(NetIncomingMessage));
			}
			else
			{
				// create "Authentication" field
				FieldDefinition authField = new FieldDefinition("Authentication", serialization.VariableType.Name, true);
				partialClass.fields.Add(authField);

				// create "GetAuthenticator" override method
				MethodDefinition getAuthMethod = new MethodDefinition("GetAuthenticator");
				getAuthMethod.ReturnType = $"Action<{nameof(NetOutgoingMessage)}>";
				getAuthMethod.accessModifier = MethodAccessModifier.Protected;
				getAuthMethod.modifier = MethodModifier.Override;
				getAuthMethod.instructions.AddInstruction($"return w => {serialization.serializeMethod.DeclaringType.Name}.{serialization.serializeMethod.Name}(Authentication, w);");
				partialClass.methods.Add(getAuthMethod);

				// import all required types
				partialClass.Import(typeof(Action<>), typeof(NetOutgoingMessage));
			}

			partialClass.Import(serialization.VariableType);

			NetworkScopeProcessor.WriteClass(partialClass);
		}

		public static void ProcessServer(Type t)
		{
			Type authObjType = GetServerAuthenticationObjectType(t);

			if (authObjType == null)
				return;

			IVariableSerialization serialization = GetAuthVariableSerialization(authObjType, t);

			if (serialization == null)
				return;

			GenerateSerialization(serialization, t, true);
		}

		#region Helpers

		private static Authenticator GetClientAuthenticatorAttribute(Type clientType)
		{
			object[] attrs = clientType.GetCustomAttributes(typeof(Authenticator), false);

			if (attrs.Length != 0)
				return (Authenticator)attrs[0];
			return null;
		}

		private static Type GetClientAuthenticationObjectType(Authenticator authAttr)
		{
			Type[] inter = authAttr.AuthenticatorType.GetInterfaces();

			if (inter.Length != 0 &&
				inter[0].IsGenericType &&
				inter[0].GetGenericTypeDefinition() == typeof(INetworkAuthenticator<,>))
				return inter[0].GetGenericArguments()[1];
			return null;
		}
		
		private static Type GetServerAuthenticationObjectType(Type serverType)
		{
			foreach (Type interfaceType in serverType.GetInterfaces())
			{
				if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(INetworkAuthenticator<,>))
					return interfaceType.GetGenericArguments()[1];
			}
			return null;
		}

		private static IVariableSerialization GetAuthVariableSerialization(Type authObjectType, Type requestingType)
		{
			// validate type
			if (authObjectType == null)
			{
				Debug.LogWarning($"Could not add Authenticator for {requestingType.Name}. Make sure your authenticator interface requires INetworkAuthenticator<TPeer,T>.");
				return null;
			}

			// make sure type can be serialized
			IVariableSerialization authSerialization = NetworkVariableProcessor.GetVariableSerialization(authObjectType, false, NetworkVariableProcessor.VariableType.Auto);

			if (authSerialization == null)
			{
				Debug.LogWarning($"Could not add Authenticator for {requestingType.Name} because the type used in the Authenticator '{authObjectType.Name}' is not serializable. Consider using a [{nameof(NetworkSerialization)}] attribute on your class.");
				return null;
			}
			if (authSerialization.isValueType)
			{
				Debug.LogWarning($"Could not add Authenticator for {requestingType.Name} because the type used in the Authenticator '{authObjectType.Name}' is a value type.");
				return null;
			}

			return authSerialization;
		}
		#endregion
	}
}
