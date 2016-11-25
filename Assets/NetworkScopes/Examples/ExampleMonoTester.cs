
namespace MyCompany
{
	using UnityEngine;
	using NetworkScopes;
	using NetworkScopes.UNet;
	using Network = NetworkScopes.Network;

	public class ExampleMonoTester : MonoBehaviour
	{
		protected NetworkServer server;
		protected IClientProvider client;

		void Start()
		{
			server = Network.CreateServer<UNetServerProvider>(7000);
			client = Network.CreateClient<UNetClientProvider>("127.0.0.1", 7000);
		}
	}
}