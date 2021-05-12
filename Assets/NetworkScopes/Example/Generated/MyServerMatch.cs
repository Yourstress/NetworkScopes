
using System;

namespace NetworkScopes.Examples
{
	[Generated]
	public class MyServerMatch : MyServerMatch_Abstract
	{
		// custom properties
		public string matchName;

		public event Action<MyServerMatch> OnMatchDestroyed = delegate {};

		protected override void OnPeerExited(INetworkPeer peer)
		{
			if (peers.Count == 0)
				DestroyScope();
		}

		protected override void Test1()
		{
			NSDebug.Log($"Server received Test1");
		}

		protected override void Test2(string str)
		{
			NSDebug.Log($"Server received Test2 " + str);
		}

		protected override int Test3()
		{
			int value = (new Random().Next() % 100) + 1;
			NSDebug.Log($"Server received Test3 - sending back random number between 1-100: " + value);
			return value;
		}

		protected override void LeaveMatch()
		{
			RemovePeer(SenderPeer, true);
		}

		void DestroyScope()
		{
			server.UnregisterScope(this);
			OnMatchDestroyed(this);
		}
	}
}
