using System;
using LiteNetLib.Utils;

namespace NetworkScopes
{
    public class SignalWriter : NetDataWriter, ISignalWriter
    {
        public SignalWriter() : base(true)
        {
        }

        public SignalWriter(short channelId) : base(true)
        {
            Write(channelId);
        }

        public void Write(bool value) => Put(value);
        public void Write(byte value) => Put(value);
        public void Write(byte[] value) => Put(value);
        public void Write(sbyte value) => Put(value);
        public void Write(char value) => Put(value);
        public void Write(float value) => Put(value);
        public void Write(double value) => Put(value);

        public void Write(short value) => Put(value);
        public void Write(int value) => Put(value);
        public void Write(long value) => Put(value);
        public void Write(ushort value) => Put(value);
        public void Write(uint value) => Put(value);
        public void Write(ulong value) => Put(value);

        public void Write(string value) => Put(value);

        public void Write(DateTime dateTime) => Put(dateTime.ToBinary());
    }
}