using UnityEngine;
using NetworkScopes;
using Network = NetworkScopes.Network;
using NetworkScopes.UNet;

public class NetworkScopeTester : MonoBehaviour
{
	void Start()
	{
		Network.CreateServer<UNetServerProvider>(7000);
		Network.CreateClient<UNetClientProvider>("127.0.0.1", 7000).enableLogging = true;
	}
}
