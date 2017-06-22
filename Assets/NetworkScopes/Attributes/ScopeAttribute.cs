using System;

namespace NetworkScopes
{
	public enum SignalReceiveType
	{
		Event,
		AbstractMethod,
	}

	public enum EntityType
	{
		Server,
		Client,
	}

	public class ScopeAttribute : Attribute
	{
		public EntityType entityType;
		public Type otherScopeType;
		public SignalReceiveType defaultReceiveType;

		public ScopeAttribute(EntityType entityType, Type otherScope, SignalReceiveType defaultReceiveType = SignalReceiveType.Event)
		{
			otherScopeType = otherScope;
			this.defaultReceiveType = defaultReceiveType;
		}
	}

	public class ServerScopeAttribute : ScopeAttribute
	{
		public ServerScopeAttribute(Type otherScope, SignalReceiveType defaultReceiveType = SignalReceiveType.Event) : base(EntityType.Server, otherScope, defaultReceiveType) {}
	}

	public class ClientScopeAttribute : ScopeAttribute
	{
		public ClientScopeAttribute(Type otherScope, SignalReceiveType defaultReceiveType = SignalReceiveType.Event) : base(EntityType.Server, otherScope, defaultReceiveType) {}
	}
}