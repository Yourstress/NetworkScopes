
namespace NetworkScopes
{
	using Lidgren.Network;

	public interface INetworkObject<T> : INetworkVariable<T> where T : class, new()
	{
		void Initialize(INetworkVariableSender sender, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod);
	}

	public class NetworkObject<T> : NetworkVariable<T>, INetworkObject<T> where T : class, new()
	{
		protected NetworkSerialization.SerializeObjectDelegate<T> _serialize;
		protected NetworkSerialization.DeserializeObjectDelegate<T> _deserialize;

		public NetworkObject() { }

		public NetworkObject(T initialValue)
		{
			_value = initialValue;
		}

		public void Initialize(INetworkVariableSender sender, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod)
		{
			Initialize(sender, objectId);

			_serialize = serializeMethod;
			_deserialize = deserializeMethod;
		}

		public override void Read(NetIncomingMessage reader)
		{
			if (_value == null)
				_value = new T();

			// null if nothing else was sent
			if (reader.Position == reader.LengthBits)
				_value = null;
			// read data
			else
				_deserialize(_value, reader);

			RaiseOnChanged();
		}

		public override void Write(NetOutgoingMessage writer)
		{
			// write data if the value isn't null
			if (_value != null)
				_serialize(_value, writer);
		}

		public static implicit operator NetworkObject<T>(T value)
		{
			return new NetworkObject<T>(value);
		}

		public static implicit operator T(NetworkObject<T> networkObject)
		{
			return networkObject.Value;
		}

		public static NetworkObject<T> FromObjectID(int objectId)
		{
			return new NetworkObject<T>() { objectId = objectId };
		}
	}
}
