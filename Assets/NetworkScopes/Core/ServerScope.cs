using System;
using System.Collections.Generic;
using NetworkScopes.Utilities;

namespace NetworkScopes
{
	public abstract class ServerScope<TPeer, TScopeSender> : ServerScope<TScopeSender>
		where TScopeSender : IScopeSender
		where TPeer : INetworkPeer
	{
		protected new TPeer SenderPeer => (TPeer)base.SenderPeer;
		
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
	}
	
	public abstract class ServerScope<TScopeSender> : IServerScope, IDisposable
		where TScopeSender : IScopeSender
	{
		public string name { get { return GetType().Name; } }
		public bool isActive { get; private set; }

		protected abstract TScopeSender GetScopeSender();

		private IServerSignalProvider _signalProvider;

		protected readonly PeerTarget peerTarget = new PeerTarget();

		public List<INetworkPeer> peers { get; private set; }

		public ScopeIdentifier scopeIdentifier { get; private set; }
		public ScopeChannel channel { get; private set; }

		/// <summary>
		/// The scope to hand over the peers to when removed from this scope.
		/// </summary>
		public IServerScope fallbackScope;

		protected INetworkPeer SenderPeer { get; private set; }

		public IScopeRegistrar scopeRegistrar { get; private set; }

		// stores NetworkPromise objects awaiting peer responses
		private readonly Dictionary<INetworkPeer, NetworkPromiseHandler> peerPromiseHandlers = new Dictionary<INetworkPeer, NetworkPromiseHandler>();

		private ShortGenerator _channelGenerator;

		protected ServerScope()
		{
			peers = new List<INetworkPeer>();
		}

		void IDisposable.Dispose()
		{
			_channelGenerator.DeallocateValue(channel);
		}

		public void InitializeServerScope(IServerScopeProvider scopeProvider, ScopeIdentifier serverScopeIdentifier, ShortGenerator channelGenerator)
		{
			scopeRegistrar = scopeProvider;
			scopeIdentifier = serverScopeIdentifier;
			channel = channelGenerator.AllocateValue();
			_channelGenerator = channelGenerator;
			_signalProvider = scopeProvider;

			SignalMethodBinder.BindScope(this);

			isActive = true;
		}

		public void AddPeer(INetworkPeer peer)
		{
			if (peers.Contains(peer))
				throw new Exception("Peer already exists in this scope.");

			peers.Add(peer);

			// register to disconnect event to remove the peer from this scope when disconnected
			peer.OnDisconnect += RemovePeer;

			// send entered event
			ServerScopeUtility.SendEnterScopeMessage(peer, _signalProvider, this);

			// notify inheritor class of this peer's entry
			OnPeerEntered(peer);
		}

		public void RemovePeer(INetworkPeer peer)
		{
			if (!peers.Remove(peer))
				throw new Exception("Peer has not been added to this scope and cannot be removed.");

			// unregister disconnect event upon removal of peer
			peer.OnDisconnect -= RemovePeer;

			// remove peer promises handler (if one exists)
			peerPromiseHandlers.Remove(peer);

			// notify inheritor class of this peer's exit
			OnPeerExited(peer);

			// send exited event (only if the peer is still connected)
			if (!peer.isDestroyed)
			{
				ServerScopeUtility.SendExitScopeMessage(peer, _signalProvider, this);

				if (fallbackScope != null)
					fallbackScope.AddPeer(peer);
			}
		}

		public void RemoveAllPeers()
		{
			// remove all peers in reverse order to avoid List element copying
			for (var x = peers.Count - 1; x >= 0; x--)
				RemovePeer(peers[x]);
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
			ISignalWriter signal = _signalProvider.CreateSignal(channel);
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
			_signalProvider.SendSignal(peerTarget, signal);
		}

		protected void ReceivePromise(ISignalReader reader)
		{
			int promiseID = reader.ReadInt32();

			GetPromiseHandler(SenderPeer).DequeueAndReceivePromise(promiseID, reader);
		}

		void IServerScope.ProcessSignal(ISignalReader signal, INetworkPeer peer)
		{
			// assign the sender before dispatching the call to the scope's parent class
			SenderPeer = peer;

			SignalMethodBinder.Invoke(this, GetType(), signal);
		}
	}
}