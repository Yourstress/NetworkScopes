
namespace NetworkScopes
{
	public static class Network
	{
		public static NetworkServer CreateBareboneServer()
		{
			return new NetworkServer();
		}

		public static NetworkServer CreateServer<TServerProvider>(int listenPort) where TServerProvider : IServerProvider, new()
		{
			NetworkServer server = new NetworkServer();

			TServerProvider provider = server.AddServerProvider<TServerProvider>();
			provider.StartListening(listenPort);

			return server;
		}

		public static TClientProvider CreateBareboneClient<TClientProvider>() where TClientProvider : IClientProvider, new()
		{
			return new TClientProvider();
		}

		public static TClientProvider CreateClient<TClientProvider>(string serverHostname, int serverPort) where TClientProvider : IClientProvider, new()
		{
			TClientProvider client = new TClientProvider();

			client.Connect(serverHostname, serverPort);

			return client;
		}
	}
}