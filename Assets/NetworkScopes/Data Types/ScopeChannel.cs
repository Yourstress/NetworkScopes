using System;
using UnityEditor;

namespace NetworkScopes
{
	public struct ScopeChannel : IEquatable<short>
	{
		private short _value;

		public const short SystemChannel = short.MinValue;
		public const short FirstCustomChannel = short.MinValue + 1;
		public const short LastCustomChannel = short.MaxValue;

		public static implicit operator ScopeChannel(short value)
		{
			return new ScopeChannel { _value = value };
		}
		public static implicit operator short(ScopeChannel scopeChannel)
		{
			return scopeChannel._value;
		}

		public bool Equals(short other)
		{
			return _value == other;
		}

		public bool Equals(ScopeChannel obj)
		{
			return _value.Equals(obj);
		}

		public override int GetHashCode()
		{
			return _value.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("[Channel {0}]", _value);
		}
	}
}