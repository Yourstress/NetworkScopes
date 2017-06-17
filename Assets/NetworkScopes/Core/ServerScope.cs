using System;
using System.Collections.Generic;
using NetworkScopes.Utilities;
using UnityEngine;

namespace NetworkScopes
{
	public abstract class ServerScope<TScopeSender> : IServerScope where TScopeSender : IScopeSender
	{
		public string name { get { return GetType().Name; } }
		public bool isActive { get; private set; }

		protected abstract TScopeSender GetScopeSender();

		private IServerSignalProvider _signalProvider;

		private PeerTarget peerTarget = new PeerTarget();

		public List<INetworkPeer> peers { get; private set; }

		public ScopeIdentifier scopeIdentifier { get; private set; }
		public ScopeChannel currentChannel { get; private set; }

		protected ServerScope()
		{
			peers = new List<INetworkPeer>();
		}

		public void InitializeServerScope(IServerSignalProvider signalProvider, ScopeIdentifier scopeIdentifier, ScopeChannel scopeChannel)
		{
			this.scopeIdentifier = scopeIdentifier;
			this.currentChannel = scopeChannel;
			_signalProvider = signalProvider;

			SignalMethodBinder.BindScope(GetType());

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

			// notify inheritor class of this peer's exit
			OnPeerExited(peer);
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

		// TODO: pass down signal options (which scope and which signal method)
		protected ISignalWriter CreateSignal(int signalID)
		{
			// return writer based on the current network medium (service provider)
			ISignalWriter signal = _signalProvider.CreateSignal(currentChannel);
			signal.WriteInt32(signalID);
			return signal;
		}

		protected void SendSignal(ISignalWriter signal)
		{
			// TODO: send out the signal using the service provider
			_signalProvider.SendSignal(peerTarget, signal);
		}

		public void ReceiveSignal(ISignalReader reader)
		{
			Debug.Log("Server received " + reader);
			// TODO: read the signal id and find the method associated

			// TODO: read parameters from the reader based on the expected methods

			// TODO: call method with parameters
		}
	}
}