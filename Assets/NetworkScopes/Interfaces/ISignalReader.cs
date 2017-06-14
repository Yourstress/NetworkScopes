namespace NetworkScopes
{
	public interface ISignalReader
	{
		string ReadString();
		short ReadShort();
		byte ReadByte();
		int ReadInt32();
	}
}