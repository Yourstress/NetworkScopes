
using NetworkScopes;
using UnityEngine;

public class ExampleClientLobby : ClientScope<ExamplePeer, ExampleServerLobby>
{
	[Signal]
	public NetworkSingleEvent<string> OnPlayerInvite;

	public string[] availableMatches { get; private set; }

	[Signal]
	public void OnMatchList(string[] matches)
	{
		availableMatches = matches;
	}

	[Signal]
	public void OnMatchNotFound(string matchName)
	{
		Debug.LogFormat("Could not join the match '{0}' because it doesn't exist", matchName);
	}

	[Signal]
	public void OnMatchAlreadyExists(string matchName)
	{
		Debug.LogFormat("Could not create the match '{0}' because it already exists", matchName);
	}
}
