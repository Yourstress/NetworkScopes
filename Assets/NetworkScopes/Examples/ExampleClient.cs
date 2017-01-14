
namespace MyCompany
{
	using NetworkScopes;
	using NetworkScopes.UNet;

	public class ExampleClient
	{
		IClientProvider client;

		public ExampleClient()
		{
			client = Network.CreateBareboneClient<UNetClientProvider>();

			client.Connect("127.0.0.1", 7000);
		}
	}
}