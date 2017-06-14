
using System;

namespace NetworkScopes
{
	public struct ScopeIdentifier : IEquatable<byte>
	{
		private byte _value;

		public static implicit operator ScopeIdentifier(byte value)
		{
			return new ScopeIdentifier { _value = value };
		}
		public static implicit operator byte(ScopeIdentifier scopeIdentifier)
		{
			return scopeIdentifier._value;
		}

		public bool Equals(byte other)
		{
			return _value == other;
		}

		public bool Equals(ScopeIdentifier other)
		{
			return _value == other._value;
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("[Scope {0}]", _value);
		}
	}
}