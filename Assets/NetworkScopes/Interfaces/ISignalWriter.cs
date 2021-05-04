namespace NetworkScopes
{
	public interface ISignalWriter
	{
		void Write(string value);
		void Write(short value);
		void Write(byte value);
		void Write(int value);
	}
}