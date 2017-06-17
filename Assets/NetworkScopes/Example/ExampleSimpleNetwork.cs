
using MyExamples;
using NetworkScopes;
using UnityEngine;

public class ExampleSimpleNetwork : MonoBehaviour
{
	private NetworkServer _server = NetworkServer.CreateLidgrenServer();
	private NetworkClient _client = NetworkClient.CreateLidgrenClient();

	public int port = 7000;

	void Start()
	{
		_server.RegisterScope<ExampleServerScope>(1);
		_client.RegisterScope<ExampleClientScope>(1);
	}

	void OnGUI()
	{
		DrawServer();
		DrawClient();
	}

	private void DrawClient()
	{
		GUILayout.Label(string.Format("Connection status: {0}", _client.IsConnected ? "Online":"Offline"));

		if (!_client.IsConnected)
		{
			if (GUILayout.Button("Connect"))
			{
				_client.Connect("127.0.0.1", port);
			}
		}
		else
		{
			if (GUILayout.Button("Disconnect"))
				_client.Disconnect();
		}
	}

	private void DrawServer()
	{
		GUILayout.Label(string.Format("Server status: {0}", _server.IsListening ? "Running" : "Stopped"));

		if (!_server.IsListening)
		{
			if (GUILayout.Button("Start"))
			{
				_server.StartListening(port);
			}
		}
		else
		{
			if (GUILayout.Button("Stop"))
			{
				_server.StopListening();
			}
		}
	}
}
