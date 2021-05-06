using System;

namespace NetworkScopes
{
	public interface ISignalReader
	{
		bool ReadBoolean();
		byte ReadByte();
		byte[] ReadByteArray();
		sbyte ReadSByte();
		char ReadChar();
		float ReadFloat();
		double ReadDouble();
		
		short ReadInt16();
		int ReadInt32();
		long ReadInt64();
		ushort ReadUInt16();
		uint ReadUInt32();
		ulong ReadUInt64();
		
		string ReadString();
		
		DateTime ReadDateTime();
	}
}