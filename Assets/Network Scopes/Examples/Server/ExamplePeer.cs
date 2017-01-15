
using NetworkScopes;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ExamplePeer : NetworkPeer
{
	public static ExampleServerLobby Lobby { get; private set; }

	public string UserName { get; private set; }

	public void SetAuthenticated(string userName)
	{
		UserName = userName;
	}
}