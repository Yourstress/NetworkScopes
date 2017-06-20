using System;

namespace NetworkScopes
{
    public static class SignalExtensions
    {
        public static ScopeChannel ReadScopeChannel(this ISignalReader reader)
        {
            return reader.ReadShort();
        }

        public static void WriteScopeChannel(this ISignalWriter writer, ScopeChannel channel)
        {
            writer.WriteShort(channel);
        }

        public static ScopeIdentifier ReadScopeIdentifier(this ISignalReader reader)
        {
            return reader.ReadByte();
        }

        public static void WriteScopeIdentifier(this ISignalWriter writer, ScopeIdentifier channel)
        {
            writer.WriteByte(channel);
        }

        public static int ReadPromiseID(this ISignalReader reader)
        {
            return reader.ReadInt32();
        }

        public static void WritePromiseIDFromReader(this ISignalWriter writer, ISignalReader reader)
        {
            writer.WriteInt32(reader.ReadInt32());
        }

        private const string _stringType = "String";
        private const string _shortType = "Int16";
        private const string _byteType = "Byte";
        private const string _int32Type = "Int32";

        public static void WriteValue(this ISignalWriter writer, object value)
        {
            Type type = value.GetType();
            switch (type.Name)
            {
                case _stringType:
                    writer.WriteString((string) value);
                    break;
                case _shortType:
                    writer.WriteShort((short) value);
                    break;
                case _byteType:
                    writer.WriteByte((byte) value);
                    break;
                case _int32Type:
                    writer.WriteInt32((int) value);
                    break;
                default:
                    throw new Exception(string.Format("The type {0} is not serializable.", value.GetType()));
            }
        }

        public static void WriteObject<T>(this ISignalWriter writer, T value) where T : ISerializable
        {
            value.Serialize(writer);
        }

        public static T ReadValue<T>(this ISignalReader reader) where T : IComparable
        {
            Type type = typeof(T);
            switch (type.Name)
            {
                case _stringType:
                    return (T)(object)reader.ReadString();
                case _shortType:
                    return (T)(object)reader.ReadShort();
                case _byteType:
                    return (T)(object)reader.ReadByte();
                case _int32Type:
                    return (T)(object)reader.ReadInt32();
                default:
                    throw new Exception(string.Format("The type {0} is not serializable.", type));
            }
        }

        public static T ReadObject<T>(this ISignalReader reader) where T : ISerializable, new()
        {
            T obj = new T();
            obj.Deserialize(reader);
            return obj;
        }
    }
}