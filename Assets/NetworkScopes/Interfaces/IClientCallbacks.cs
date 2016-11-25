
namespace NetworkScopes
{
	public interface IClientCallbackHandler
	{
		void OnConnect();
		void OnDisconnect();
		void OnReceiveRaw(INetworkReader reader);
	}
}