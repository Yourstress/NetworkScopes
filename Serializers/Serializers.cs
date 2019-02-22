
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using Lidgren.Network;

	public static class SignalSender
	{
		public static void SendRaw<T>(this IMessageSender sender, int signalType, T value, Action<T,NetOutgoingMessage> serializer)
		{
			NetOutgoingMessage writer = sender.CreateWriter(signalType);
			serializer(value, writer);
			sender.PrepareAndSendWriter(writer);
		}
	}

	public class ValueSerializerTypes
	{
		static readonly Dictionary<Type, Type> typeSerializerTypes = new Dictionary<Type, Type>
		{
			{ typeof(int), typeof(IntSerializer) },
			{ typeof(float), typeof(FloatSerializer) },
			{ typeof(double), typeof(DoubleSerializer) },
			{ typeof(long), typeof(LongSerializer) },
			{ typeof(string), typeof(StringSerializer) },
			{ typeof(bool), typeof(BooleanSerializer) }
		};

		public static Type GetSerializerClass(Type type)
		{
			Type serializerType = null;
			typeSerializerTypes.TryGetValue(type, out serializerType);
			return serializerType;
		}
	}

	public static class IntSerializer
	{
		public static void NetworkSerialize(int value, NetOutgoingMessage writer) => writer.Write(value);
		public static int NetworkDeserialize(NetIncomingMessage reader) => reader.ReadInt32();
	}

	public static class FloatSerializer
	{
		public static void NetworkSerialize(float value, NetOutgoingMessage writer) => writer.Write(value);
		public static float NetworkDeserialize(NetIncomingMessage reader) => reader.ReadFloat();
	}

	public static class DoubleSerializer
	{
		public static void NetworkSerialize(double value, NetOutgoingMessage writer) => writer.Write(value);
		public static double NetworkDeserialize(NetIncomingMessage reader) => reader.ReadDouble();
	}

	public static class ShortSerializer
	{
		public static void NetworkSerialize(short value, NetOutgoingMessage writer) => writer.Write(value);
		public static short NetworkDeserialize(NetIncomingMessage reader) => reader.ReadInt16();
	}

	public static class LongSerializer
	{
		public static void NetworkSerialize(long value, NetOutgoingMessage writer) => writer.Write(value);
		public static long NetworkDeserialize(NetIncomingMessage reader) => reader.ReadInt64();
	}

	public static class StringSerializer
	{
		public static void NetworkSerialize(string value, NetOutgoingMessage writer) => writer.Write(value);
		public static string NetworkDeserialize(NetIncomingMessage reader) => reader.ReadString();
	}

	public static class BooleanSerializer
	{
		public static void NetworkSerialize(bool value, NetOutgoingMessage writer) => writer.Write(value);
		public static bool NetworkDeserialize(NetIncomingMessage reader) => reader.ReadBoolean();
	}
}
