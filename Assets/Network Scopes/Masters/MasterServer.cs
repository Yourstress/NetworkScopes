
namespace NetworkScopes
{
	using System;
	using UnityEngine.Networking;
	using System.Collections.Generic;
	using UnityEngine;

	public abstract class MasterServer<TPeer> where TPeer : NetworkPeer
	{
		protected List<TPeer> peers = new List<TPeer>();

		public List<TPeer> Peers { get { return peers; } }

		public int NumberOfPeers { get { return peers.Count; } }

		public event Action<TPeer> OnPeerConnected = delegate {};
//		public event Action<TPeer> OnPeerDisconnected = delegate {};
		
		private const string _loopbackAddress = "127.0.0.1";
		
		public void RedirectPeer(TPeer peer, string hostName, int port)
		{
			// if we're trying to redirect to the same server, fake the reconnection on the server side instead
			if (hostName.Contains(_loopbackAddress) && port == NetworkServer.listenPort)
			{
				// trigger disconnect event for this peer in order process any cleanup events registered
				peer.SoftDisconnect();
				
				
				// trigger connecting back to the server
				NetworkMessage nm = new NetworkMessage() { conn = peer };
				OnPeerConnectedMsg(nm);
			}
			else
				peer.Redirect(hostName, port);
		}

		public TPeer FindPeer(Func<TPeer,bool> predicate)
		{
			for (int x = 0; x < peers.Count; x++)
			{
				if (predicate(peers[x]))
					return peers[x];
			}

			return default(TPeer);
		}

		#region Scope Registration
		private HashSet<BaseServerScope<TPeer>> registeredScopes = new HashSet<BaseServerScope<TPeer>>();
		private BaseServerScope<TPeer> defaultScope = null;

		private ShortGenerator msgTypeGenerator = new ShortGenerator(100,short.MaxValue);

		public TServerScope RegisterScope<TServerScope>(byte scopeIdentifier, bool setAsDefault) where TServerScope : BaseServerScope<TPeer>, new()
		{
			TServerScope scope = new TServerScope();

			scope.SetAutomaticMsgType(scopeIdentifier, msgTypeGenerator);

			// initialize the scope with the MasterServer instance
			scope.Initialize(this);

			registeredScopes.Add(scope);

			// set as default scope for newly connected players if specified
			if (setAsDefault)
				defaultScope = scope;

			return scope;
		}

		public void UnregisterScope(BaseServerScope<TPeer> scope)
		{
			// don't allow unregistering default scope
			if (defaultScope == scope)
				throw new Exception("Failed to unregister Scope because it is the default Scope");
			
			// remove from registered scopes
			if (registeredScopes.Remove(scope))
			{
				// make sure to clean-up before letting go of this scope
				scope.Dispose();
			}
			else
				Debug.LogWarningFormat("Failed to remove the Scope {0} because it was not registered with the MasterServer.", scope.GetType());
		}
		#endregion

		public void StartServer(int port)
		{
			HostTopology topology = new HostTopology(MasterClient.CreateConnectionConfig(), 3000);
			NetworkServer.Configure(topology);

			NetworkServer.SetNetworkConnectionClass<TPeer>();

			NetworkServer.Listen(port);

			NetworkServer.RegisterHandler(MsgType.Connect, OnPeerConnectedMsg);
			NetworkServer.RegisterHandler(MsgType.Disconnect, OnPeerDisconnectedMsg);
		}

		// Called (event-handler) when a Peer connects organically
		void OnPeerConnectedMsg(NetworkMessage msg)
		{
			// create the peer
			TPeer peer = (TPeer)msg.conn;

			// add to peers
			peers.Add(peer);

			peer.OnDisconnect += Peer_OnDisconnect;

			// notify event listeners
			OnPeerConnected(peer);

			// add this peer to the default scope
			defaultScope.AddPeer(peer, true);
		}

		// Called (event-handler) when a Peer disconnects organically
		void OnPeerDisconnectedMsg(NetworkMessage msg)
		{
			// create the peer
			TPeer peer = (TPeer)msg.conn;

			// force the peer to disconnect across all scopes, as well clean-up within the Master (Peer_OnDisconnect)
			peer.ForceDisconnect(false);
		}

		// Called (event-handler) by NetworkPeer upon disconnection (by ForceDisconnect method, whether organically or deliberately)
		void Peer_OnDisconnect (NetworkPeer netPeer)
		{
			netPeer.OnDisconnect -= Peer_OnDisconnect;

			// remove from collection
			if (!peers.Remove(netPeer as TPeer))
			{
				Debug.Log("<color=red>Failed</color> to remove peer " + netPeer);
			}

			// dispose channel buffers
			netPeer.Dispose();
		}
	}
}