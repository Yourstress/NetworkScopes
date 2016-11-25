
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

			server.OnPeerEntityConnected += Server_OnPeerEntityConnected;
			server.OnPeerEntityDisconnected += Server_OnPeerEntityDisconnected;

//			ExampleServerMatchScope matchScope = server.CreateScope<ExampleServerMatchScope>(0, true);
//			server.UseAuthenticator<ExampleAuthenticatorScope>(matchScope);

			client = Network.CreateClient<UNetClientProvider>("127.0.0.1", 7000);

			client.OnConnected += Client_OnConnected;
			client.OnDisconnected += Client_OnDisconnected;
		}

		void Server_OnPeerEntityConnected (PeerEntity obj)
		{
			Debug.Log("Peer connected");
		}
		
		void Server_OnPeerEntityDisconnected (PeerEntity obj)
		{
			Debug.Log("Peer disconnected");
		}

		void Client_OnConnected ()
		{
			Debug.Log("Connected!");
		}
		
		void Client_OnDisconnected ()
		{
			Debug.Log("Disconnected");
		}
	}
}