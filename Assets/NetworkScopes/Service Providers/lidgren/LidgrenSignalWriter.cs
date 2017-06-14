using Lidgren.Network;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public class LidgrenSignalWriter : ISignalWriter
	{
		public readonly NetOutgoingMessage netMessage;

		public LidgrenSignalWriter(NetOutgoingMessage message, short scopeChannel)
		{
			netMessage = message;
			netMessage.Write(scopeChannel);
		}

		public void WriteString(string str)
		{
			netMessage.Write(str);
		}

		public void WriteShort(short value)
		{
			netMessage.Write(value);
		}

		public void WriteByte(byte value)
		{
			netMessage.Write(value);
		}

		public void WriteInt32(int value)
		{
			netMessage.Write(value);
		}
	}
}