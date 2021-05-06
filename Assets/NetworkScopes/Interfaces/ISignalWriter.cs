namespace NetworkScopes
{
	using System;
	
	public interface ISignalWriter
	{
		void Write(float value);
		void Write(string value);
		void Write(short value);
		void Write(byte value);
		void Write(int value);
		void Write(DateTime dateTime);
	}
}