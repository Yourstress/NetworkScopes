using System;

namespace NetworkScopes
{
	public struct ScopeChannel : IEquatable<short>
	{
		// Channel ranges
		public const short FirstChannel = short.MinValue + 1;
		public const short LastChannel = short.MaxValue;
		
		public const short EnterScope = 90;
		public const short ExitScope = 91;
		public const short SwitchScope = 92;
		public const short DisconnectMessage = 93;
		public const short RedirectMessage = 94;
		
		public const short FirstSystemChannel = EnterScope;
		public const short LastSystemChannel = RedirectMessage;
	
		// Class members
		private short _channelId;
		public bool IsSystemChannel => _channelId >= FirstSystemChannel && _channelId <= LastSystemChannel;

		public static bool IsSystemScope(ScopeChannel channel) => channel.IsSystemChannel;

		public static implicit operator ScopeChannel(short value)
		{
			return new ScopeChannel { _channelId = value };
		}
		public static implicit operator short(ScopeChannel scopeChannel)
		{
			return scopeChannel._channelId;
		}

		public bool Equals(short other)
		{
			return _channelId == other;
		}

		public bool Equals(ScopeChannel obj)
		{
			return _channelId.Equals(obj);
		}

		public override int GetHashCode()
		{
			return _channelId.GetHashCode();
		}

		public override string ToString()
		{
			return $"[Channel {_channelId}]";
		}
	}
}