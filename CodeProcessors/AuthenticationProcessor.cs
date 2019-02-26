using System;
using CodeGeneration;
using Lidgren.Network;

namespace NetworkScopes.CodeProcessors
{
	public static class AuthenticationProcessor
	{
		public static void ProcessClient(Type t)
		{
			Authenticator authAttr = GetClientAuthenticatorAttribute(t);

			if (authAttr == null)
				return;

			GenerateSerialization(t, false);
		}

		private static void GenerateSerialization(Type clientOrServerType, bool isServer)
		{
			// create partial class
			ClassDefinition partialClass = new ClassDefinition(clientOrServerType.Name, clientOrServerType.Namespace, true);
			partialClass.FileName = "G-" + clientOrServerType.Name;

			Type objType = typeof(object);

			if (isServer)
			{
				// create auth override method
				MethodDefinition authMethod = new MethodDefinition("AuthenticatePeer");
				authMethod.ReturnType = "bool";
				authMethod.accessModifier = MethodAccessModifier.Protected;
				authMethod.modifier = MethodModifier.Override;

				Type peerType = clientOrServerType.BaseType.GetGenericArguments()[0];
				authMethod.parameters.Add(new ParameterDefinition("peer", peerType.Name));
				authMethod.parameters.Add(new ParameterDefinition("msg", nameof(NetIncomingMessage)));

				authMethod.instructions.AddInstruction(string.Format("{0} auth = new {0}();", objType.Name));
				authMethod.instructions.AddInstruction("try");
				authMethod.instructions.AddInstruction("{");
//				authMethod.instructions.AddInstruction("\t"+serialization.CreateNetworkDeserializeCall("auth", "msg"));
//				authMethod.instructions.AddInstruction("\t"+serialization.CreateNetworkDeserializeCall("msg"));
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
				FieldDefinition authField = new FieldDefinition("Authentication", objType.Name, true);
				partialClass.fields.Add(authField);

				// create "GetAuthenticator" override method
				MethodDefinition getAuthMethod = new MethodDefinition("GetAuthenticator");
				getAuthMethod.ReturnType = $"Action<{nameof(NetOutgoingMessage)}>";
				getAuthMethod.accessModifier = MethodAccessModifier.Protected;
				getAuthMethod.modifier = MethodModifier.Override;
//				getAuthMethod.instructions.AddInstruction($"return w => {objType.DeclaringType.Name}.{serialization.serializeMethod.Name}(Authentication, w);");
				partialClass.methods.Add(getAuthMethod);

				// import all required types
				partialClass.Import(typeof(Action<>), typeof(NetOutgoingMessage));
			}

			partialClass.Import(objType);

			NetworkScopeProcessor.WriteClass(partialClass);
		}

		#region Helpers

		private static Authenticator GetClientAuthenticatorAttribute(Type clientType)
		{
			object[] attrs = clientType.GetCustomAttributes(typeof(Authenticator), false);

			if (attrs.Length != 0)
				return (Authenticator)attrs[0];
			return null;
		}
		#endregion
	}
}
