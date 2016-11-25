
namespace NetworkScopes
{
	public interface INetworkWriter
	{
		void WriteString(string value);
		void WriteBoolean(bool value);
		void WriteInt32(int value);
		void WriteInt16(short value);
		void WriteByte(byte value);
		void WriteSingle(float value);
		void WriteChar(char value);
	}
}