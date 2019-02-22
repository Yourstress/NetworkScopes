
using System.Linq;
using Lidgren.Network;

namespace NetworkScopes
{
	using System.Collections.Generic;

//	#define SHOW_SEND_ERRORS

	public static class NetConnectionListPool
	{
		private static readonly Stack<IList<NetConnection>> pooledLists = new Stack<IList<NetConnection>>();
		
		public static IList<NetConnection> GetConnectionList<TPeer>(IEnumerable<TPeer> peers) where TPeer : NetworkPeer
		{
			IList<NetConnection> connectionList;

			if (pooledLists.Count > 0)
			{
				connectionList = pooledLists.Pop();
			}
			else
			{
				connectionList = new List<NetConnection>();
			}
			
			foreach (TPeer peer in peers)
			{
				if (!peer.isConnected)
					continue;
				
				connectionList.Add(peer.connection);
			}

			return connectionList;
		}

		public static void PoolConnectionList(IList<NetConnection> connectionList)
		{
			connectionList.Clear();
			pooledLists.Push(connectionList);
		}
	}

	public static class ScopeUtils
	{
		public static void SendRawMessage(NetOutgoingMessage msg, NetworkPeer peer)
		{
			if (!peer.isConnected)
				return;

			peer.connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
		}

		public static void SendRawMessage<TPeer>(NetOutgoingMessage msg, IEnumerable<TPeer> peers, NetServer masterNetServer) where TPeer : NetworkPeer
		{
			List<NetConnection> connectionList = NetConnectionListPool.GetConnectionList(peers).ToList();

			if (connectionList.Count > 0)
				masterNetServer.SendMessage(msg, connectionList, NetDeliveryMethod.ReliableOrdered, 0);
			NetConnectionListPool.PoolConnectionList(connectionList);
		}

		public static int GetConsistentHashCode(this string source)
		{
			int hash1 = 5381;
			int hash2 = hash1;

			int c;
			for (int x = 0; x < source.Length; x+= 2)
			{
				c = source[x];

				hash1 = ((hash1 << 5) + hash1) ^ c;

				if (x+1 < source.Length)
					c = source[x+1];

				hash2 = ((hash2 << 5) + hash2) ^ c;
			}

			return hash1 + (hash2 * 1566083941);
		}

		public static void SendScopeSwitchedMessage(IScope prevScope, IScope newScope, NetworkPeer peer)
		{
			NetOutgoingMessage msg = peer.connection.Peer.CreateMessage();

			msg.Write(ScopeMsgType.SwitchScope);

			// 1. msgType: Send prev scope channel
			msg.Write(prevScope.msgType);

			// 2. msgType: Send new scope channel
			msg.Write(newScope.msgType);

			// 3. scopeIdentifier: The value which identifier the counterpart (new) client scope
			msg.Write(newScope.scopeIdentifier);

			// 4. NetworkVariables: write any NetworkObject/NetworkValue objects registered on the newly joined scope
			newScope.WriteNetworkVariables(msg);

			SendRawMessage(msg, peer);
		}

		public static void SendScopeEnteredMessage(IScope scope, NetworkPeer peer)
		{
			NetOutgoingMessage msg = peer.connection.Peer.CreateMessage();

			msg.Write(ScopeMsgType.EnterScope);

			// 1. scopeIdentifier: The value which identifier the counterpart client class
			msg.Write(scope.msgType);

			// 2. msgType: Determines which channel to communicate on
			msg.Write(scope.scopeIdentifier);

			// 3. NetworkVariables: write any NetworkObject/NetworkValue objects registered on the newly joined scope
			scope.WriteNetworkVariables(msg);

			SendRawMessage(msg, peer);
		}

		public static void SendScopeExitedMessage(IScope scope, NetworkPeer peer)
		{
			NetOutgoingMessage msg = peer.connection.Peer.CreateMessage();

			msg.Write(ScopeMsgType.ExitScope);

			// 1. msgType: Determines which channel to communicate on
			msg.Write(scope.msgType);

			SendRawMessage(msg, peer);
		}
	}
}
