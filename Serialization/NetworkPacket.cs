using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using ProtoBuf;

namespace NetworkScopes
{
	public class IncomingNetworkPacket : NetworkPacket
	{
		private readonly BinaryReader reader;
		private int nextProtobufFieldNumber = 1;

		public IncomingNetworkPacket(byte[] data) : base(data)
		{
			reader = new BinaryReader(stream);
		}

		public T ReadObject<T>() where T : new()
		{
			// try to deserialize it using ISerializable if found
			T obj = Serialization.TryDeserialize<T>(this);
			if (obj != null)
				return obj;

			// velse use Protobuf
			return Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Base128, nextProtobufFieldNumber++);
		}

		public T[] ReadArray<T>() where T : new()
		{
			int length = ReadInt();
			T[] array = new T[length];

			for (int x = 0; x < length; x++)
				array[x] = ReadObject<T>();

			return array;
		}

		public bool ReadBoolean()
		{
			return reader.ReadBoolean();
		}

		public byte ReadByte()
		{
			return reader.ReadByte();
		}

		public short ReadShort()
		{
			return reader.ReadInt16();
		}

		public int ReadInt()
		{
			return reader.ReadInt32();
		}

		public string ReadString()
		{
			return reader.ReadString();
		}

		public override void Dispose()
		{
			reader.Dispose();
			base.Dispose();
		}
	}

	public static class Serialization
	{
		private static readonly Dictionary<Type, bool> serializableTypes = new Dictionary<Type, bool>();

		public static bool TrySerialize(object obj, OutgoingNetworkPacket packet)
		{
			if (IsSerializable(obj.GetType()))
			{
				((ISerializable)obj).Write(packet);
				return true;
			}

			return false;
		}

		public static T TryDeserialize<T>(IncomingNetworkPacket packet) where T : new()
		{
			if (IsSerializable(typeof(T)))
			{
				T obj = new T();
				((ISerializable)obj).Read(packet);
				return obj;
			}

			return default(T);
		}

		public static bool IsSerializable(Type type)
		{
			bool isSerializable;

			if (!serializableTypes.TryGetValue(type, out isSerializable))
			{
				isSerializable = Array.IndexOf(type.GetInterfaces(), typeof(ISerializable)) != -1;

				serializableTypes[type] = isSerializable;
			}

			return isSerializable;
		}
	}

	public class OutgoingNetworkPacket : NetworkPacket
	{
		private readonly BinaryWriter writer;
		private int nextProtobufFieldNumber = 1;

		public OutgoingNetworkPacket(int capacity = 16) : base(capacity)
		{
			writer = new BinaryWriter(stream);
		}

		public void WriteObject<T>(T obj)
		{
			// try to serialize it using ISerializable if found, else use Protobuf
			if (!Serialization.TrySerialize(obj, this))
				Serializer.SerializeWithLengthPrefix(stream, obj, PrefixStyle.Base128, nextProtobufFieldNumber++);
		}

		public void WriteArray<T>(T[] array)
		{
			Write(array.Length);

			for (int x = 0; x < array.Length; x++)
				WriteObject<T>(array[x]);
		}

		public void Write(bool value)
		{
			writer.Write(value);
		}

		public void Write(byte value)
		{
			writer.Write(value);
		}

		public void Write(short value)
		{
			writer.Write(value);
		}

		public void Write(int value)
		{
			writer.Write(value);
		}

		public void Write(string value)
		{
			writer.Write(value);
		}

		public override void Dispose()
		{
			writer.Dispose();
			base.Dispose();
		}
	}

	public class NetworkPacket : IDisposable
	{
		public readonly MemoryStream stream;

		protected NetworkPacket(int capacity)
		{
			stream = new MemoryStream(capacity);
		}

		protected NetworkPacket(byte[] data)
		{
			stream = new MemoryStream(data);
			stream.Position = 0;
		}

		public static OutgoingNetworkPacket CreateOutgoingPacket(short msgType, int signalType)
		{
			OutgoingNetworkPacket packet = new OutgoingNetworkPacket(32);
			packet.Write(msgType);
			packet.Write(signalType);
			return packet;
		}

		public static IncomingNetworkPacket CreateIncomingPacket(byte[] data)
		{
			return new IncomingNetworkPacket(data);
		}

		public static IncomingNetworkPacket FromMessage(NetIncomingMessage msg)
		{
			byte[] data = msg.ReadBytes(msg.LengthBytes-msg.PositionInBytes);
			IncomingNetworkPacket packet = new IncomingNetworkPacket(data);
			return packet;
		}

		#region Lidgren Support
		public NetOutgoingMessage CreateOutgoingMessage(NetPeer peer)
		{
			NetOutgoingMessage msg = peer.CreateMessage();

			byte[] buffer = stream.GetBuffer();
			msg.Write(buffer, 0, (int)stream.Length);

			return msg;
		}
		#endregion

		public virtual void Dispose()
		{
			stream?.Dispose();
		}
	}
}