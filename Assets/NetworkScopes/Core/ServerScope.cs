using System;
using System.Collections.Generic;
using NetworkScopes.Utilities;

namespace NetworkScopes
{
	public abstract class ServerScope<TPeer, TScopeSender> : ServerScope<TScopeSender>
		where TScopeSender : IScopeSender
		where TPeer : INetworkPeer
	{
		protected new INetworkServer<TPeer> server;
		
		protected new TPeer SenderPeer => (TPeer)base.SenderPeer;

		public override void InitializeServerScope(INetworkServer networkServer, ScopeIdentifier serverScopeIdentifier, ChannelGenerator channelGenerator)
		{
			base.InitializeServerScope(networkServer, serverScopeIdentifier, channelGenerator);
			server = base.server as INetworkServer<TPeer>;
		}

		protected virtual void OnPeerEntered(TPeer peer) {}
		protected virtual void OnPeerExited(TPeer peer) {}

		protected override void OnPeerEntered(INetworkPeer peer)
		{
			OnPeerEntered((TPeer)peer);
		}

		protected override void OnPeerExited(INetworkPeer peer)
		{
			OnPeerExited((TPeer)peer);
		}

		public TPeer FindPeer(Func<TPeer, bool> findFunc)
		{
			for (int x = 0; x < peers.Count; x++)
			{
				TPeer peer = (TPeer)peers[x];
				if (findFunc(peer))
					return peer;
			}

			return default;
		}

		public TPeer FindPeerInAnyScope(Func<TPeer, bool> findFunc) => server.FindPeer(findFunc);
	}
	
	public abstract class ServerScope<TScopeSender> : IServerScope
		where TScopeSender : IScopeSender
	{
		public string name { get { return GetType().Name; } }
		public bool IsActive { get; private set; }

		protected abstract TScopeSender GetScopeSender();

		protected readonly PeerTarget peerTarget = new PeerTarget();

		protected readonly List<INetworkPeer> peers = new List<INetworkPeer>();
		public IReadOnlyList<INetworkPeer> Peers => peers;

		public ScopeIdentifier scopeIdentifier { get; private set; }
		public ScopeChannel channel { get; private set; }

		/// <summary>
		/// The scope to hand over the peers to when removed from this scope.
		/// </summary>
		public IServerScope fallbackScope { get; set; }

		protected INetworkPeer SenderPeer { get; private set; }
		
		protected INetworkServer server;

		// stores NetworkPromise objects awaiting peer responses
		private readonly Dictionary<INetworkPeer, NetworkPromiseHandler> peerPromiseHandlers = new Dictionary<INetworkPeer, NetworkPromiseHandler>();

		private ChannelGenerator _channelGenerator;

		public virtual void InitializeServerScope(INetworkServer networkServer, ScopeIdentifier serverScopeIdentifier, ChannelGenerator channelGenerator)
		{
			server = networkServer;
			scopeIdentifier = serverScopeIdentifier;
			channel = channelGenerator.AllocateValue();
			_channelGenerator = channelGenerator;

			SignalMethodBinder.BindScope(this);

			IsActive = true;
		}
		
		void IDisposable.Dispose()
		{
			_channelGenerator.DeallocateValue(channel);
		}

		public virtual void AddPeer(INetworkPeer peer, bool sendEnterMessage)
		{
			if (peers.Contains(peer))
				throw new Exception("Peer already exists in this scope.");

			peers.Add(peer);

			// register to disconnect event to remove the peer from this scope when disconnected
			peer.OnDisconnect += OnPeerDisconnected;

			// send entered event
			if (sendEnterMessage)
				ServerScopeUtility.SendEnterScopeMessage(peer, server, this);

			// notify inheritor class of this peer's entry
			OnPeerEntered(peer);
		}

		public virtual void RemovePeer(INetworkPeer peer, bool sendExitMessage)
		{
			if (!peers.Remove(peer))
				throw new Exception("Peer has not been added to this scope and cannot be removed.");

			// unregister disconnect event upon removal of peer
			peer.OnDisconnect -= OnPeerDisconnected;

			// remove peer promises handler (if one exists)
			peerPromiseHandlers.Remove(peer);

			// notify inheritor class of this peer's exit
			OnPeerExited(peer);

			// send exited event (only if the peer is still connected)
			if (!peer.IsDestroyed && sendExitMessage)
			{
				ServerScopeUtility.SendExitScopeMessage(peer, server, this);

				if (fallbackScope != null)
					fallbackScope.AddPeer(peer, true);
			}
		}

		public void HandoverPeer(INetworkPeer peer, IServerScope targetScope)
		{
			// tell the peer about this handover
			ServerScopeUtility.SendSwitcheScopeMessage(peer, server, this, targetScope);

			// remove peer from this scope
			RemovePeer(peer, false);

			// add peer to target scope
			targetScope.AddPeer(peer, false);
		}

		protected virtual void OnPeerDisconnected(INetworkPeer peer)
		{
			RemovePeer(peer, false);
		}

		NetworkPromiseHandler GetPromiseHandler(INetworkPeer peer)
		{
			// if this peer doesn't have a promise handler, create one and add it to the dictionary (to be cleaned when peer is removed)
			if (!peerPromiseHandlers.TryGetValue(peer, out NetworkPromiseHandler ph))
				peerPromiseHandlers[peer] = ph = new NetworkPromiseHandler();

			return ph;
		}

		protected virtual void OnPeerEntered(INetworkPeer peer)
		{

		}

		protected virtual void OnPeerExited(INetworkPeer peer)
		{

		}

		public TScopeSender SendToPeer(INetworkPeer peer)
		{
			peerTarget.TargetPeer = peer;
			return GetScopeSender();
		}

		public TScopeSender ReplyToPeer()
		{
			peerTarget.TargetPeer = SenderPeer;
			return GetScopeSender();
		}

		public TScopeSender SendToPeers(IEnumerable<INetworkPeer> peers)
		{
			peerTarget.TargetPeerGroup = peers;
			return GetScopeSender();
		}

		public TScopeSender SendToAll()
		{
			peerTarget.TargetPeerGroup = peers;
			return GetScopeSender();
		}

		public TScopeSender SendToAllExcept(INetworkPeer peer)
		{
			peerTarget.TargetPeerGroup = peers;
			peerTarget.TargetPeerGroupException = peer;
			return GetScopeSender();
		}

		protected ISignalWriter CreateSignal(int signalID)
		{
			// return writer based on the current network medium (service provider)
			ISignalWriter signal = server.CreateSignal(channel);
			signal.Write(signalID);
			return signal;
		}

		protected ISignalWriter CreatePromiseSignal(int signalID, INetworkPromise promise)
		{
			ISignalWriter signal = CreateSignal(signalID);
			signal.Write(GetPromiseHandler(SenderPeer).EnqueuePromise(promise));
			return signal;
		}

		protected void SendSignal(ISignalWriter signal, INetworkPeer targetPeer)
		{
			peerTarget.TargetPeer = targetPeer;
			SendSignal(signal);
		}

		protected void SendSignal(ISignalWriter signal)
		{
			// TODO: send out the signal using the service provider
			server.SendSignal(peerTarget, signal);
		}

		protected void ReceivePromise(ISignalReader reader)
		{
			int promiseID = reader.ReadInt32();

			GetPromiseHandler(SenderPeer).DequeueAndReceivePromise(promiseID, reader);
		}

		void IServerScope.ProcessSignal(ISignalReader signal, INetworkPeer peer)
		{
			// ignore messges from unregistered peers
			if (!peers.Contains(peer))
				return;
			
			// assign the sender before dispatching the call to the scope's parent class
			SenderPeer = peer;

			SignalMethodBinder.Invoke(this, GetType(), signal);
		}
	}
}