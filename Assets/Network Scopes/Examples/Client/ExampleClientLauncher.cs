
using UnityEngine;

public class ExampleClientLauncher : MonoBehaviour
{
	public string host = "127.0.0.1";
	public int port = 1234;

	private string playerName = "";

	ExampleClient client;

	void Start()
	{
		client = new ExampleClient();

		client.Connect(host, port);
	}

	void OnGUI()
	{
		if (!client.IsConnected && !client.IsConnecting)
		{
			DrawOfflineGUI();
		}
		else if (client.IsConnecting)
		{
			GUILayout.Label("Connecting...");
		}
		else
		{
			if (client.Lobby.IsActive)
			{
				DrawLobbyGUI();
			}
			if (client.Match.IsActive)
			{
				DrawMatchGUI();
			}
		}
	}

	void DrawOfflineGUI()
	{
		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label("IP Address", GUILayout.Width(80));
			host = GUILayout.TextField(host, 15, GUILayout.Width(120));
		}

		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label("Port", GUILayout.Width(80));

			string stringPort = GUILayout.TextField(port.ToString(), 15, GUILayout.Width(120));
			int.TryParse(stringPort, out port);
		}

		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label("Player Name", GUILayout.Width(80));
			playerName = GUILayout.TextField(playerName, GUILayout.Width(120));
		}

		GUILayout.Button("Connect");
	}

	string matchName = "";
	void DrawLobbyGUI()
	{
		GUILayout.Label("Lobby Scope");
		GUILayout.Space(10);

		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label("Game Name");
			matchName = GUILayout.TextField(matchName, GUILayout.Width(120));

			if (GUILayout.Button("Create"))
				client.Lobby.SendToServer.CreateMatch(matchName);
		}

		if (client.Lobby.availableMatches != null)
		{
			for (int x = 0; x < client.Lobby.availableMatches.Length; x++)
			{
				using (new GUILayout.HorizontalScope())
				{
					GUILayout.Label(client.Lobby.availableMatches[x], GUILayout.Width(120));

					if (GUILayout.Button("Join"))
						client.Lobby.SendToServer.JoinMatch(client.Lobby.availableMatches[x]);
				}
			}
		}
	}

	void DrawMatchGUI()
	{
		GUILayout.Label("Match Scope");
		GUILayout.Space(10);

		if (GUILayout.Button("Send Play Action"))
		{
			client.Match.SendToServer.Play();
		}
		if (GUILayout.Button("Send Auto-Serialized Object"))
		{
			ExampleObject obj = new ExampleObject();
			obj.num = 10;
			obj.flt = 99.99f;
			obj.str = "Ten";
			obj.numNonSerialized = 99;

			Debug.Log("Sending auto-serialized object: " + obj);

			client.Match.SendToServer.TestAutoSerializedObj(obj);
		}
		if (GUILayout.Button("Leave Match"))
		{
			client.Match.SendToServer.LeaveMatch();
		}
	}
}