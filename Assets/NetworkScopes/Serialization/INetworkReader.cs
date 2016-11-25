
namespace NetworkScopes
{
	public interface INetworkReader
	{
		string ReadString();
		bool ReadBoolean();
		int ReadInt32();
		short ReadInt16();
		byte ReadByte();
		float ReadSingle();
		char ReadChar();
	}
}