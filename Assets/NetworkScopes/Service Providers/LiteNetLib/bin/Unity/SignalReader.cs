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

        public short ReadShort()
        {
            return reader.GetShort();
        }

        public byte ReadByte()
        {
            return reader.GetByte();
        }

        public bool ReadBoolean()
        {
            return reader.GetBool();
        }

        public char ReadChar()
        {
            return reader.GetChar();
        }

        public ushort ReadUInt16()
        {
            return reader.GetUShort();
        }

        public short ReadInt16()
        {
            return reader.GetShort();
        }

        public long ReadInt64()
        {
            return reader.GetLong();
        }

        public ulong ReadUInt64()
        {
            return reader.GetULong();
        }

        public int ReadInt32()
        {
            return reader.GetInt();
        }

        public uint ReadUInt()
        {
            return reader.GetUInt();
        }

        public float ReadFloat()
        {
            return reader.GetFloat();
        }

        public double ReadDouble()
        {
            return reader.GetDouble();
        }

        public string ReadString()
        {
            return reader.GetString();
        }

        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(reader.GetLong());
        }
    }
}