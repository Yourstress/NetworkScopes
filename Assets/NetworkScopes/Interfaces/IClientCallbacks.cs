
namespace NetworkScopes
{
	public interface IClientInitializer
	{
		short scopeChannel { get; }

		void Initialize(short scopeChannel);
	}

	public interface IClientCallbackHandler
	{
		void OnConnect();
		void OnDisconnect();
		void OnReceiveRaw(INetworkReader reader);
	}
}