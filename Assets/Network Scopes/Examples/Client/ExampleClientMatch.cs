
using NetworkScopes;
using UnityEngine;

public class ExampleClientMatch : ClientScope<ExamplePeer, ExampleServerMatch>
{
	[Signal]
	public void PlayerJoined(string playerName)
	{
		Debug.Log(playerName + " has joined the game");
	}

	[Signal]
	public void PlayerLeft(string playerName)
	{
		Debug.Log(playerName + " has left the game");
	}

	[Signal]
	public void PlayerPlayed(string playerName)
	{
		Debug.Log(playerName + " has just played");
	}
}