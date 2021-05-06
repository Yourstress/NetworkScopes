
using System;
using System.Collections.Generic;

namespace NetworkScopes
{
    public static class SignalExtensions
    {
        #region Data Types

        public static ScopeChannel ReadScopeChannel(this ISignalReader reader)
        {
            return reader.ReadInt16();
        }

        public static void WriteScopeChannel(this ISignalWriter writer, ScopeChannel channel)
        {
            writer.Write(channel);
        }

        public static ScopeIdentifier ReadScopeIdentifier(this ISignalReader reader)
        {
            return reader.ReadByte();
        }

        public static void WriteScopeIdentifier(this ISignalWriter writer, ScopeIdentifier channel)
        {
            writer.Write(channel);
        }

        #endregion

        #region Promises

        public static int ReadPromiseID(this ISignalReader reader)
        {
            return reader.ReadInt32();
        }

        public static void WritePromiseIDFromReader(this ISignalWriter writer, ISignalReader reader)
        {
            writer.Write(reader.ReadInt32());
        }

        #endregion

        #region Generic Objects

        private const string _boolType = "Boolean";
        private const string _stringType = "String";
        private const string _shortType = "Int16";
        private const string _byteType = "Byte";
        private const string _int32Type = "Int32";

        public static void WriteValue(this ISignalWriter writer, object value)
        {
            Type type = value.GetType();
            switch (type.Name)
            {
                case _boolType:
                    writer.Write((bool) value);
                    break;
                case _stringType:
                    writer.Write((string) value);
                    break;
                case _shortType:
                    writer.Write((short) value);
                    break;
                case _byteType:
                    writer.Write((byte) value);
                    break;
                case _int32Type:
                    writer.Write((int) value);
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
                case _boolType:
                    return (T)(object)reader.ReadBoolean();
                case _stringType:
                    return (T)(object)reader.ReadString();
                case _shortType:
                    return (T)(object)reader.ReadInt16();
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

        #endregion

        #region Arrays

        public static T[] ReadObjectArray<T>(this ISignalReader reader) where T : ISerializable, new()
        {
            int length = reader.ReadInt32();
            T[] array = new T[length];

            for (int x = 0; x < length; x++)
            {
                T obj = array[x] = new T();
                obj.Deserialize(reader);
            }
            return array;
        }

        public static void WriteObjectArray<T>(this ISignalWriter writer, T[] array) where T : ISerializable
        {
            writer.Write(array.Length);

            for (int x = 0; x < array.Length; x++)
            {
                array[x].Serialize(writer);
            }
        }

        public static List<T> ReadObjectList<T>(this ISignalReader reader) where T : ISerializable, new()
        {
            int length = reader.ReadInt32();
            List<T> list = new List<T>(length);

            for (int x = 0; x < length; x++)
            {
                T obj = new T();
                obj.Deserialize(reader);

                list.Add(obj);
            }
            return list;
        }

        public static void WriteObjectList<T>(this ISignalWriter writer, List<T> list) where T : ISerializable
        {
            writer.Write(list.Count);

            for (int x = 0; x < list.Count; x++)
                list[x].Serialize(writer);
        }

        public static Dictionary<TKey,TValue> ReadObjectDictionary<TKey,TValue>(this ISignalReader reader) where TKey : ISerializable, new() where TValue : ISerializable, new()
        {
            int length = reader.ReadInt32();
            Dictionary<TKey,TValue> dictionary = new Dictionary<TKey, TValue>(length);

            for (int x = 0; x < length; x++)
            {
                TKey key = new TKey();
                key.Deserialize(reader);

                TValue value = new TValue();
                value.Deserialize(reader);

                dictionary[key] = value;
            }
            return dictionary;
        }

        public static void WriteObjectDictionary<TKey,TValue>(this ISignalWriter writer, Dictionary<TKey,TValue> dictionary) where TKey : ISerializable where TValue : ISerializable
        {
            writer.Write(dictionary.Count);

            foreach (KeyValuePair<TKey,TValue> kvp in dictionary)
            {
                kvp.Key.Serialize(writer);
                kvp.Value.Serialize(writer);
            }
        }

        #endregion
    }
}