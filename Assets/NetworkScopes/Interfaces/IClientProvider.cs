
namespace NetworkScopes
{
	using System;

	public interface IClientProvider
	{
		bool isConnected { get; }
		bool isConnecting { get; }
		
		event Action OnConnected;
		event Action OnDisconnected;

		void Connect(string hostname, int port);
		void Disconnect();
		void SendRaw(INetworkWriter writer);
	}
}