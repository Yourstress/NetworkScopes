
using System.Collections.Generic;
using UnityEngine;

namespace NetworkScopes
{
	using System;
	using System.Reflection;
	using Lidgren.Network;

	public enum NetworkSerializeSettings
	{
		PublicFieldsOnly,
		PublicFieldsAndProperties,
		AllFields,
		AllFieldsAndProperties,
		OptIn,
		Custom,
	}

	public class NetworkSerializer<T> : NetworkSerializer
	{
		protected Action<T, NetOutgoingMessage> serializeAction;
		protected Action<T, NetIncomingMessage> deserializeAction;

		public NetworkSerializer(MethodInfo serializeMethod, MethodInfo deserializeMethod)
		{
			serializeAction = (Action<T, NetOutgoingMessage>) Delegate.CreateDelegate(typeof(Action<T, NetOutgoingMessage>), serializeMethod);
			deserializeAction = (Action<T, NetIncomingMessage>) Delegate.CreateDelegate(typeof(Action<T, NetIncomingMessage>), deserializeMethod);
		}

		public void Serialize(T value, NetOutgoingMessage writer)
		{
			Debug.Log("Target is " + serializeAction.Target);
			serializeAction(value, writer);
		}

		public void Deserialize(T value, NetIncomingMessage reader)
		{
			deserializeAction(value, reader);
		}
	}

	public class NetworkSerializer
	{
		private static readonly Dictionary<Type, NetworkSerializer> serializers = new Dictionary<Type, NetworkSerializer>();

		private static NetworkSerializer Create<T>(MethodInfo serializeMethod, MethodInfo deserializeMethod)
		{
			return new NetworkSerializer<T>(serializeMethod, deserializeMethod);
		}

		private static NetworkSerializer<T> Get<T>()
		{
			NetworkSerializer serializer;
			if (!serializers.TryGetValue(typeof(T), out serializer))
			{
				Type t = typeof(T);
				MethodInfo serializeMethod = t.GetMethod("NetworkSerialize", new[] { typeof(T), typeof(NetOutgoingMessage) });
				MethodInfo deserializeMethod = t.GetMethod("NetworkDeserialize", new[] { typeof(T), typeof(NetIncomingMessage) });

				if (serializeMethod != null && deserializeMethod != null)
					serializer = NetworkSerializer.Create<T>(serializeMethod, deserializeMethod);

				serializers[t] = serializer;
			}

			if (serializer == null)
				Debug.Log("Could not find runtime serializer for " + typeof(T));

			return (NetworkSerializer<T>)serializer;
		}

		public static void Write<T>(T value, NetOutgoingMessage writer)
		{
			Get<T>().Serialize(value, writer);
		}

		public static void Read<T>(T value, NetIncomingMessage reader)
		{
			Get<T>().Deserialize(value, reader);
		}
	}

	public class NetworkSerialization : Attribute
	{
		public NetworkSerializeSettings SerializeSetting;

		public delegate void SerializeObjectDelegate<T>(T value, NetOutgoingMessage writer) where T : class, new();
		public delegate void DeserializeObjectDelegate<T>(T value, NetIncomingMessage reader) where T : class, new();

		public delegate void SerializeValueDelegate<T>(T value, NetOutgoingMessage writer);
		public delegate T DeserializeValueDelegate<T>(NetIncomingMessage reader);

		public NetworkSerialization(NetworkSerializeSettings serializeSetting = NetworkSerializeSettings.PublicFieldsOnly)
		{
			SerializeSetting = serializeSetting;
		}

		public void GetSerializedMembers(Type paramType, out FieldInfo[] fields, out PropertyInfo[] props)
		{
			switch (SerializeSetting)
			{
			case NetworkSerializeSettings.AllFieldsAndProperties:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				props = paramType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				break;

			case NetworkSerializeSettings.AllFields:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				props = null;
				break;

			case NetworkSerializeSettings.PublicFieldsAndProperties:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public);
				props = paramType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				break;

			case NetworkSerializeSettings.PublicFieldsOnly:
				fields = paramType.GetFields(BindingFlags.Instance | BindingFlags.Public);
				props = null;
				break;

			default:
				throw new Exception("Failed to serialized using the setting " + SerializeSetting);
			}
		}
	}
}