
using MyExamples;
using NetworkScopes;
using UnityEngine;

public class ExampleSimpleNetwork : MonoBehaviour
{
	protected NetworkServer _server = NetworkServer.CreateLidgrenServer();
	protected NetworkClient _client = NetworkClient.CreateLidgrenClient();

	public int port = 7000;

	void Start()
	{
		RegisterScopes();
	}

	public virtual void RegisterScopes()
	{
		_server.RegisterScope<ExampleServerScope>(1);
		_client.RegisterScope<ExampleClientScope>(1);
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
		GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
		DrawServerGUI();
		GUILayout.EndVertical();
		GUILayout.Space(18);
		GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
		DrawClientGUI();
		GUILayout.EndVertical();
		GUILayout.EndVertical();
	}

	protected virtual void DrawClientGUI()
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

	protected virtual void DrawServerGUI()
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
