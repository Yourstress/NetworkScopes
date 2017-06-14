namespace NetworkScopes
{
	public interface ISignalWriter
	{
		void WriteString(string value);
		void WriteShort(short value);
		void WriteByte(byte value);
		void WriteInt32(int value);
	}
}