
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;

	public abstract class BaseServer<TPeer> where TPeer : NetworkPeer
	{
		protected List<TPeer> peers = new List<TPeer>();

		public List<TPeer> Peers { get { return peers; } }

		public int NumberOfPeers { get { return peers.Count; } }

		public event Action<TPeer> OnPeerConnected = delegate {};
//		public event Action<TPeer> OnPeerDisconnected = delegate {};

		private const string _loopbackAddress = "127.0.0.1";

		public bool isServerStarted { get; private set; }
		public int listenPort;

		public abstract IMessageWriter CreateWriter (short msgType);
		public abstract void SendWriter (IMessageWriter writer, TPeer target);
		public abstract void SendWriter (IMessageWriter writer, IEnumerable<TPeer> targets);

		#region Scope Handler Registration
		public delegate void ScopeHandlerDelegate(IMessageReader reader, TPeer sender);
		private Dictionary<short,ScopeHandlerDelegate> _handlers = new Dictionary<short, ScopeHandlerDelegate> ();

		public void RegisterScopeHandler (short msgType, ScopeHandlerDelegate handler)
		{
			_handlers [msgType] = handler;
		}

		public void UnregisterScopeHandler (short msgType)
		{
			_handlers.Remove (msgType);
		}

		public ScopeHandlerDelegate GetScopeHandler(short msgType)
		{
			return _handlers[msgType];
		}
		#endregion

		public void StartServer(int port)
		{
			if (isServerStarted)
				throw new Exception("Server is already started");

			listenPort = port;

			StartServer();
		}


		protected abstract void StartServer();
		public abstract void StopServer();

		#region Scope Registration
		private HashSet<BaseServerScope<TPeer>> registeredScopes = new HashSet<BaseServerScope<TPeer>>();
		private BaseServerScope<TPeer> defaultScope = null;

		private ShortGenerator msgTypeGenerator = new ShortGenerator(100,short.MaxValue);

		public TServerScope RegisterScope<TServerScope>(byte scopeIdentifier, bool setAsDefault) where TServerScope : BaseServerScope<TPeer>, new()
		{
			TServerScope scope = new TServerScope();
			
			// initialize the scope with the MasterServer instance
			scope.Initialize(this);

			scope.SetAutomaticMsgType(scopeIdentifier, msgTypeGenerator);

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
				ScopeUtils.Log("Failed to remove the Scope {0} because it was not registered with the MasterServer.", scope.GetType());
		}
		#endregion

		#region Peer Operations

		public void RedirectPeer(TPeer peer, string hostName, int port)
		{
			// if we're trying to redirect to the same server, fake the reconnection on the server side instead
			if (hostName.Contains(_loopbackAddress) && port == listenPort)
			{
				// trigger disconnect event for this peer in order process any cleanup events registered
				peer.SoftDisconnect();

				// trigger connecting back to the server
				PeerConnected(peer);
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
		#endregion

		// Called (event-handler) when a Peer connects organically
		protected void PeerConnected(TPeer peer)
		{
			// add to peers
			peers.Add(peer);

			peer.OnDisconnect += Peer_OnDisconnect;

			// notify event listeners
			OnPeerConnected(peer);

			// add this peer to the default scope
			defaultScope.AddPeer(peer, true);
		}

		// Called (event-handler) when a Peer disconnects organically
		protected void PeerDisconnected(TPeer peer)
		{
			// force the peer to disconnect across all scopes, as well clean-up within the Master (Peer_OnDisconnect)
			peer.ForceDisconnect(false);
		}

		protected void PeerSentSignal(IMessageReader reader, TPeer sender)
		{
			short scopeChannel = reader.ReadInt16();
			_handlers[scopeChannel].Invoke(reader, sender);
		}

		// Called (event-handler) by NetworkPeer upon disconnection (by ForceDisconnect method, whether organically or deliberately)
		protected virtual void Peer_OnDisconnect (NetworkPeer netPeer)
		{
			netPeer.OnDisconnect -= Peer_OnDisconnect;

			// remove from collection
			if (!peers.Remove(netPeer as TPeer))
				ScopeUtils.Log("<color=red>Failed</color> to remove peer " + netPeer);

			// dispose channel buffers
			netPeer.Dispose();
		}
	}
}