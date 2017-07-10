using System.Collections.Generic;
using NetworkScopes;

[Generated]
public class MyServerLobby : MyServerLobby_Abstract
{
	private List<MyServerMatch> matches = new List<MyServerMatch>();

	protected override int GetOnlinePlayerCount()
	{
		return peers.Count;
	}

	protected override void JoinAnyMatch()
	{
		MyServerMatch match = GetOrCreateMatch();

		ReplyToPeer().FoundMatch();

		// this is a built-in method that adds this peer to the scope - this will trigger the "Entered Scope" event on both ends.
		match.AddPeer(SenderPeer);

		RemovePeer(SenderPeer);
	}

	private MyServerMatch GetOrCreateMatch()
	{
		// find a non-full running match and return it
		foreach (MyServerMatch myServerMatch in matches)
		{
			if (!myServerMatch.isFull)
				return myServerMatch;
		}

		// if none was found, register a new match scope
		MyServerMatch match = scopeRegistrar.RegisterScope<MyServerMatch>(2);

		// make it so removing a peer from the match (via the RemovePeer method) adds the peer back to this scope
		match.fallbackScope = this;

		matches.Add(match);

		return match;
	}
}
