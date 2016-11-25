
namespace MyCompany
{
	using NetworkScopes;
	using NetworkScopes.UNet;

	public class ExampleServer
	{
		NetworkServer server;

		public ExampleServer()
		{
			server = new NetworkServer();
			server.AddServerProvider<UNetServerProvider>().StartListening(7000);

//			ExampleServerMatch match = server.CreateScope<ExampleServerMatch>(0, true);

//			server.UseAuthenticator<MyAuthenticator>(match);
		}
			
	}
}