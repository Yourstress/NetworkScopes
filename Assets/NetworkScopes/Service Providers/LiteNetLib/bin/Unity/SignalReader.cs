
using System;
using LiteNetLib.Utils;

namespace NetworkScopes
{
    public class SignalReader : NetDataReader, ISignalReader
    {
        private readonly NetDataReader reader;

        public SignalReader(NetDataReader reader)
        {
            this.reader = reader;
        }

        public SignalReader(byte[] data)
        {
            this.reader = new NetDataReader(data);
        }
        
        public bool ReadBoolean() => reader.GetBool();
        public byte ReadByte() => reader.GetByte();
        public byte[] ReadByteArray() => reader.GetBytesWithLength();
        public sbyte ReadSByte() => reader.GetSByte();
        public char ReadChar() => reader.GetChar();
        public float ReadFloat() => reader.GetFloat();
        public double ReadDouble() => reader.GetDouble();
        
        public short ReadInt16() => reader.GetShort();
        public int ReadInt32() => reader.GetInt();
        public long ReadInt64() => reader.GetLong();
        public ushort ReadUInt16() => reader.GetUShort();
        public uint ReadUInt32() => reader.GetUInt();
        public ulong ReadUInt64() => reader.GetULong();
        
        public string ReadString() => reader.GetString();
        
        public DateTime ReadDateTime() => DateTime.FromBinary(reader.GetLong());
    }
}