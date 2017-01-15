
using NetworkScopes;
using UnityEngine;

[Scope(typeof(ExampleServerMatch))]
public partial class ExampleClientMatch : ClientScope
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