
using System.Collections.Generic;

namespace NetworkScopes.Examples
{
	[Generated]
	public class MyServerLobby : MyServerLobby_Abstract
	{
		public readonly List<MyServerMatch> matches = new();
		
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
			match.fallbackScope = this;
			match.OnMatchDestroyed += DestroyMatch;
			matches.Add(match);
			return match;
		}

		private void DestroyMatch(MyServerMatch match)
		{
			match.OnMatchDestroyed -= DestroyMatch;

			matches.Remove(match);
		}
	}
}
