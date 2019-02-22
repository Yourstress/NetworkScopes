
//#define SHOW_UNKNOWN_CHANNEL_SIGNALS



namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using Lidgren.Network;
	using NetworkScopes.Utilities;

	public abstract class MasterServer<TPeer> where TPeer : NetworkPeer, new()
	{
		public NetServer netServer { get; private set; }
		private LidgrenMessageReceiver _receiver;
		
		protected List<TPeer> peers = new List<TPeer>();

		public List<TPeer> Peers { get { return peers; } }

		public int NumberOfPeers { get { return peers.Count; } }

		public event Action<TPeer> OnPeerConnected = delegate {};
		public event Action<TPeer> OnPeerDisconnected = delegate {};
		
		private const string _loopbackAddress = "127.0.0.1";

		public NetOutgoingMessage CreateOutgoingMessage()
		{
			return netServer.CreateMessage();
		}
		
		public void RedirectPeer(TPeer peer, string hostName, int port)
		{
			// if we're trying to redirect to the same server, fake the reconnection on the server side instead
			if (hostName.Contains(_loopbackAddress) && port == netServer.Port)
			{
				// trigger disconnect event for this peer in order process any cleanup events registered
				peer.SoftDisconnect();
				
				// trigger connecting back to the server
				OnLidgrenPeerConnected(peer, null);
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

		protected virtual bool AuthenticatePeer(TPeer peer, NetIncomingMessage msg)
		{
			// default authenticator accepts all peers
			return true;
		}

		#region Scope Registration
		private HashSet<BaseServerScope<TPeer>> registeredScopes = new HashSet<BaseServerScope<TPeer>>();
		private Dictionary<short, BaseServerScope<TPeer>> scopesByMsgType = new Dictionary<short, BaseServerScope<TPeer>>();
		private BaseServerScope<TPeer> defaultScope = null;

		private readonly ShortGenerator msgTypeGenerator = new ShortGenerator(100,short.MaxValue);

		public void RegisterScope(BaseServerScope<TPeer> scope, byte scopeIdentifier, bool setAsDefault)
		{
			scope.SetAutomaticMsgType(scopeIdentifier, msgTypeGenerator);

			// initialize the scope with the MasterServer instance
			scope.Initialize(this);

			registeredScopes.Add(scope);

			// set as default scope for newly connected players if specified
			if (setAsDefault)
				defaultScope = scope;

			scopesByMsgType[scope.msgType] = scope;
		}

		public TServerScope RegisterScope<TServerScope>(byte scopeIdentifier, bool setAsDefault) where TServerScope : BaseServerScope<TPeer>, new()
		{
			TServerScope scope = new TServerScope();

			RegisterScope(scope, scopeIdentifier, setAsDefault);

			return scope;
		}

		public void UnregisterScope(BaseServerScope<TPeer> scope)
		{
			// don't allow unregistering default scope
			if (defaultScope == scope)
				throw new Exception("Failed to unregister Scope because it is the default Scope");

			scopesByMsgType.Remove(scope.msgType);
			
			// remove from registered scopes
			if (registeredScopes.Remove(scope))
			{
				// make sure to clean-up before letting go of this scope
				scope.Dispose();
			}
			else
				NetworkDebug.LogWarningFormat("Failed to remove the Scope {0} because it was not registered with the MasterServer.", scope.GetType());
		}
		#endregion

		public void StartServer(int port)
		{
			if (netServer == null)
			{
				netServer = new NetServer(LidgrenUtils.CreateServerConfiguration("Default", port));
				_receiver = new LidgrenMessageReceiver(netServer, OnReceiveMessage);
			}

			if (netServer.Status != NetPeerStatus.Running)
			{
				netServer.Start();
			}
		}

		public void StopServer()
		{
			if (netServer != null && netServer.Status == NetPeerStatus.Running)
			{
				_receiver.Dispose();
				_receiver = null;
				
				netServer.Shutdown(null);
				netServer = null;
			}
		}

		private readonly Dictionary<NetConnection, TPeer> _connectionPeers = new Dictionary<NetConnection, TPeer>();

		#region Lidgren Event Handlers
		private void OnReceiveMessage(NetIncomingMessage msg)
		{
			switch (msg.MessageType)
			{
				case NetIncomingMessageType.StatusChanged:
				{
					NetConnectionStatus status = (NetConnectionStatus) msg.ReadByte();

					TPeer peer;
					if (status == NetConnectionStatus.Connected)
					{
						peer = new TPeer();
						peer.Initialize(msg.SenderConnection);

						_connectionPeers[msg.SenderConnection] = peer;

						// trigger peer connected event
						OnLidgrenPeerConnected(peer, msg.SenderConnection.RemoteHailMessage);
					}
					else if (status == NetConnectionStatus.Disconnected)
					{
						if (_connectionPeers.TryGetValue(msg.SenderConnection, out peer))
						{
							_connectionPeers.Remove(msg.SenderConnection);

							// trigger peer disconnected event
							OnLidgrenPeerDisconnected(peer);
						}
						else
						{
							NetworkDebug.LogFormat("Failed to remove peer " + msg.SenderEndPoint.Address + " connection " + msg.SenderConnection);
						}
					}

					break;
				}
				case NetIncomingMessageType.Data:
					OnLidgrenDataReceived(msg, _connectionPeers[msg.SenderConnection]);
					break;
//				default:
//					LidgrenUtilities.ParseMessage("[Server] ", msg);
//					break;
			}
		}
		#endregion

		protected virtual void SendToDefaultScope(TPeer peer)
		{
			defaultScope.AddPeer(peer, true);
		}

		// Called (event-handler) when a Peer connects organically
		void OnLidgrenPeerConnected(TPeer peer, NetIncomingMessage msg)
		{
			// add to peers
			peers.Add(peer);

			peer.OnDisconnect += Peer_OnDisconnect;

			// notify event listeners
			OnPeerConnected(peer);

			// check authentication
			if (!AuthenticatePeer(peer, msg))
			{
				peer.DisconnectWithMessage(0);
			}
			else
			{
				// add this peer to the default scope
				SendToDefaultScope(peer);
			}
		}

		// Called (event-handler) when a Peer disconnects organically
		void OnLidgrenPeerDisconnected(TPeer peer)
		{
			// force the peer to disconnect across all scopes, as well clean-up within the Master (Peer_OnDisconnect)
			peer.ForceDisconnect(false);
		}
		
		void OnLidgrenDataReceived(NetIncomingMessage msg, TPeer sender)
		{
			short msgType = msg.ReadInt16();

			BaseServerScope<TPeer> targetScope;
			
			// in order to receive a signal, the receiving scope must be active (got an Entered Scope system message).
			if (scopesByMsgType.TryGetValue(msgType, out targetScope))
			{
				targetScope.ProcessPeerSignal(msg, sender);
			}
			else
			{
				#if SHOW_UNKNOWN_CHANNEL_SIGNALS
				int signalType = msg.PeekInt32();
				NetworkDebug.LogWarningFormat("Server could not process signal {1} on unknown channel {0}.", msgType, signalType);
				#endif
			}
		}

		// Called (event-handler) by NetworkPeer upon disconnection (by ForceDisconnect method, whether organically or deliberately)
		void Peer_OnDisconnect (NetworkPeer netPeer)
		{
			netPeer.OnDisconnect -= Peer_OnDisconnect;

			TPeer peer = (TPeer) netPeer;

			// remove from collection
			if (peers.Remove(peer))
			{
				OnPeerDisconnected(peer);
			}
			else
				NetworkDebug.Log("<color=red>Failed</color> to remove peer " + netPeer);
		}
	}
}