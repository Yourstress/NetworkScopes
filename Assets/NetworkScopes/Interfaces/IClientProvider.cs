using System;

namespace NetworkScopes
{
	public interface IClientProvider
	{
		bool IsConnecting { get; }
		bool IsConnected { get; }

		void Connect(string hostnameOrIP, int port);
		void Disconnect();

		event Action OnConnected;
		event Action OnDisconnected;
	}
}