
namespace NetworkScopes
{
	using Lidgren.Network;

	public interface INetworkValue<T> : INetworkVariable<T>
	{
		void Initialize(INetworkVariableSender sender, int objectId, NetworkSerialization.SerializeValueDelegate<T> serializeMethod, NetworkSerialization.DeserializeValueDelegate<T> deserializeMethod);
	}

	public class NetworkValue<T> : NetworkVariable<T>, INetworkValue<T>
	{
		NetworkSerialization.SerializeValueDelegate<T> _serialize;
		NetworkSerialization.DeserializeValueDelegate<T> _deserialize;

		public NetworkValue() { }

		public NetworkValue(T initialValue)
		{
			_value = initialValue;
		}

		public void Initialize(INetworkVariableSender sender, int objectId, NetworkSerialization.SerializeValueDelegate<T> serializeMethod, NetworkSerialization.DeserializeValueDelegate<T> deserializeMethod)
		{
			Initialize(sender, objectId);

			_serialize = serializeMethod;
			_deserialize = deserializeMethod;
		}

		public override void Read(NetIncomingMessage reader)
		{
			_value = _deserialize(reader);

			RaiseOnChanged();
		}

		public override void Write(NetOutgoingMessage writer)
		{
			_serialize(_value, writer);
		}

		public static implicit operator NetworkValue<T>(T value)
		{
			return new NetworkValue<T>(value);
		}

		public static implicit operator T(NetworkValue<T> networkObject)
		{
			return networkObject.Value;
		}

		public static NetworkValue<T> FromObjectID(int objectId)
		{
			return new NetworkValue<T>() { objectId = objectId };
		}
	}
}
