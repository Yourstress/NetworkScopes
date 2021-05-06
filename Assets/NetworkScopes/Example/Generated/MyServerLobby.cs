
using System.Collections.Generic;

namespace NetworkScopes.Examples
{
	[Generated]
	public class MyServerLobby : MyServerLobby_Abstract
	{
		private readonly List<MyServerMatch> matches = new();
		
		protected override void JoinAnyMatch()
		{
			// create new match
			MyServerMatch newMatch = CreateNewMatch();
			newMatch.matchName = "Match 123";
			
			HandoverPeer(SenderPeer, newMatch);
		}

		protected override bool JoinMatch(bool rankedOnly)
		{
			Debug.Log($"Server got 'JoinMatch' rankedOnly={rankedOnly} command.");
			return !rankedOnly;
		}

		private MyServerMatch CreateNewMatch()
		{
			MyServerMatch match = scopeRegistrar.RegisterScope<MyServerMatch>(1);
			matches.Add(match);
			return match;
		}

	}
}
