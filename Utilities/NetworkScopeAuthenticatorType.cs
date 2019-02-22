using System;

namespace NetworkScopes
{
	public struct NetworkScopeAuthenticatorType
	{
		public Type type;
		public bool isServer;

		public NetworkScopeAuthenticatorType(Type t, bool isServer)
		{
			type = t;
			this.isServer = isServer;
		}
	}
}