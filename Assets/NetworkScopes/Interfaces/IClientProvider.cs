
namespace NetworkScopes
{
	public interface IClientProvider
	{
		bool isConnected { get; }
		bool isConnecting { get; }

		void Connect(string hostname, int port);
		void Disconnect();
	}
}