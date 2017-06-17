using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NetworkScopes;
using UnityEditor;
using UnityEngine;

public class SimpleLobby : ExampleSimpleNetwork
{
    private MyServerLobby serverLobby;
    private MyClientLobby clientLobby;

    public override void RegisterScopes()
    {
        serverLobby = _server.RegisterScope<MyServerLobby>(1);
        clientLobby = _client.RegisterScope<MyClientLobby>(1);

        clientLobby.OnFoundMatch += OnFoundMatch;
    }

    private void OnFoundMatch(LobbyMatch match)
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
                serverLobby.SendToAll().FoundMatch(new LobbyMatch());
            }
        }

    }

    protected override void DrawClientGUI()
    {
        base.DrawClientGUI();

        DrawScope(clientLobby);
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
