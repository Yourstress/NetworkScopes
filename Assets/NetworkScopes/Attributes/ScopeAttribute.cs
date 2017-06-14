using System;

namespace NetworkScopes
{
	public enum SignalReceiveType
	{
		Event,
		AbstractMethod,
	}

	public class ScopeAttribute : Attribute
	{
		public Type otherScopeType;
		public SignalReceiveType defaultReceiveType;

		public ScopeAttribute(Type otherScope, SignalReceiveType defaultReceiveType = SignalReceiveType.Event)
		{
			otherScopeType = otherScope;
			this.defaultReceiveType = defaultReceiveType;
		}
	}
}