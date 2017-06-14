using Lidgren.Network;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public class LidgrenSignalReader : ISignalReader
	{
		public readonly NetIncomingMessage netMessage;
		
		public LidgrenSignalReader(NetIncomingMessage message)
		{
			netMessage = message;
		}
		
		public string ReadString()
		{
			return netMessage.ReadString();
		}

		public short ReadShort()
		{
			return netMessage.ReadInt16();
		}

		public byte ReadByte()
		{
			return netMessage.ReadByte();
		}

		public int ReadInt32()
		{
			return netMessage.ReadInt32();
		}
	}
}