using System;

namespace NetworkScopes
{
	public class Authenticator : System.Attribute
	{
		public Type AuthenticatorType { get; }

		public Authenticator(System.Type authenticatorType)
		{
			AuthenticatorType = authenticatorType;
		}
	}
}
