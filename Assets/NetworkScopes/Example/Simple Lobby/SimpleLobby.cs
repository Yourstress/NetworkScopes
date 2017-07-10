using NetworkScopes;
using UnityEngine;

public class SimpleLobby : ExampleSimpleNetwork
{
    public bool autoConnect = true;
    private MyServerLobby serverLobby;
    private MyClientLobby clientLobby;
    private MyClientMatch clientMatch;

    public override void RegisterScopes()
    {
        serverLobby = _server.RegisterScope<MyServerLobby>(1);
        clientLobby = _client.RegisterScope<MyClientLobby>(1);
        clientMatch = _client.RegisterScope<MyClientMatch>(2);

        clientLobby.OnFoundMatch += OnFoundMatch;

        if (autoConnect)
        {
            _server.StartListening(port);
            _client.Connect("localhost", port);
        }
    }

    private void OnFoundMatch()
    {
        Debug.Log("Match found!");
    }

    protected override void DrawServerGUI()
    {
        base.DrawServerGUI();

        DrawScope(serverLobby);

        GUILayout.BeginHorizontal();
        GUILayout.Space(18);
        GUILayout.BeginVertical();
        GUILayout.Label(string.Format("Peers ({0} online)", serverLobby.peers.Count));
        foreach (INetworkPeer peer in serverLobby.peers)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(peer.ipAddress);
            if (GUILayout.Button("Disconnect", GUILayout.ExpandWidth(false)))
                peer.Disconnect();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();


        if (_server.IsListening && _server.PeerCount > 0)
        {
            GUILayout.Label("Send Signal:");
            if (GUILayout.Button("OnFoundMatch"))
            {
                serverLobby.SendToAll().FoundMatch();
            }
        }
    }

    protected override void DrawClientGUI()
    {
        base.DrawClientGUI();

        DrawScope(clientLobby);
        DrawScope(clientMatch);



        if (_client.IsConnected)
        {
            if (clientLobby.isActive)
            {
                if (GUILayout.Button("Get Online Players"))
                    clientLobby.SendToServer.GetOnlinePlayerCount()
                        .ContinueWith(t => { Debug.Log("Online Players: " + t); });

                if (GUILayout.Button("Join Any Match"))
                    clientLobby.SendToServer.JoinAnyMatch();
            }
            if (clientMatch.isActive)
            {
                if (GUILayout.Button("Leave Match"))
                    clientMatch.SendToServer.LeaveMatch();
            }
        }
    }

    private void DrawScope(IBaseScope scope)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(scope.name);

        GUI.color = scope.isActive ? Color.green : Color.red;
        GUILayout.Label(scope.isActive ? "Active" : "Inactive");
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }
}