
namespace NetworkScopes
{
	public interface INetworkSerializable
	{
		void NetworkSerialize(INetworkWriter writer);
		void NetworkDeserialize(INetworkReader reader);
	}
}
