
using NetworkScopes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[Scope(typeof(ExampleClientLobby))]
public partial class ExampleServerLobby : ServerScope<ExamplePeer>
{
	public Dictionary<string,ExampleServerMatch> matches { get; private set; }

	public override void Initialize (MasterServer<ExamplePeer> server)
	{
		base.Initialize (server);

		// initialize the dictionary that will contain all running matches (expecting 100 running matches)
		matches = new Dictionary<string,ExampleServerMatch>(100);
		
		// create 3 empty Matches (Scopes) that peers can join and play in - you can optionally create games on demand
		matches.Add("test game 1", Master.RegisterScope<ExampleServerMatch>(2, false));
		matches.Add("test game 2", Master.RegisterScope<ExampleServerMatch>(2, false));
		matches.Add("test game 3", Master.RegisterScope<ExampleServerMatch>(2, false));
	}

	protected override void OnPeerEnteredScope (ExamplePeer peer)
	{
		// here you can perform actions when a peer enters the scope

		// get the list of available matches and send them to this Peer
		string[] matchNames = new List<string>(matches.Keys).ToArray();

		// we must explicitly target this peer when not inside a [Scope]-attributed method
		SendToPeer(peer).OnMatchList(matchNames);

		// doesn't do anything HERE - you can take it out
		base.OnPeerEnteredScope (peer);
	}

	protected override void OnPeerExitedScope (ExamplePeer peer)
	{
		// here you can perform actions when a peer exits the scope

		// doesn't do anything HERE - you can take it out
		base.OnPeerExitedScope (peer);
	}

	[Signal]
	public void JoinMatch(string matchName)
	{
		// verify that a game with the specified name actually exists
		if (matches.ContainsKey(matchName))
		{
			ExampleServerMatch match = matches[matchName];

			// hand the peer over to the specified match
			HandoverPeer(SenderPeer, match);
		}
		// if game couldn't be found, tell the peer
		else
		{
			SendToPeer(SenderPeer).OnMatchNotFound(matchName);
		}
	}

	[Signal]
	public void CreateMatch(string matchName)
	{
		// verify that a game with that name doesn't already exist
		if (!matches.ContainsKey(matchName))
		{
			// create a new Match by registering it with the Master Server instance
			ExampleServerMatch newMatch = Master.RegisterScope<ExampleServerMatch>(2, false);

			// add it to the dictionary under the given match name
			matches[matchName] = newMatch;

			// hand the peer over to the newly created match
			HandoverPeer(SenderPeer, newMatch);
		}
		// if it already exists, tell the peer
		else
		{
			ReplyToPeer().OnMatchAlreadyExists(matchName);
		}
	}
}

