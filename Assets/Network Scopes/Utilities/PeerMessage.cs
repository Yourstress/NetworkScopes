
namespace NetworkScopes
{
	public interface IMessageReader
	{
		string ReadString();
		bool ReadBoolean();
		int ReadInt32();
		short ReadInt16();
		byte ReadByte();
		float ReadSingle();
	}

	public interface IMessageWriter
	{
		void Write(string value);
		void Write(bool value);
		void Write(int value);
		void Write(short value);
		void Write(byte value);
		void Write(float value);
	}

	public class PeerMessage<TPeer> where TPeer : NetworkPeer
	{
		public TPeer peer { get; private set; }
		public IMessageReader reader { get; private set; }

		public PeerMessage(TPeer peer, IMessageReader reader)
		{
			this.peer = peer;
			this.reader = reader;
		}
	}
}