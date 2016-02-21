
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Networking;

	public abstract class BaseServerScope<TPeer> : BaseScope, IDisposable where TPeer : IScopePeer
	{
		public MasterServer<TPeer> Master { get; private set; }

		public virtual void Initialize(MasterServer<TPeer> server)
		{
			// keep a reference for future use
			Master = server;

			// initialize BaseScope
			Initialize();
		}

		#region MsgType Registration
		private ShortGenerator msgTypeGenerator;

		public void SetManualMsgType(byte scopeIdentifier, ShortGenerator msgTypeGen, short manualMsgType)
		{
			if (msgTypeGenerator != null)
				throw new Exception(string.Format("The Scope {0} has already been assigned a MsgType value", GetType()));

			this.scopeIdentifier = scopeIdentifier;
			
			// use up the value manually in the generator
			msgTypeGen.AllocateManualValue(manualMsgType);

			msgType = manualMsgType;
			msgTypeGenerator = msgTypeGen;
		}

		public void SetAutomaticMsgType(byte scopeIdentifier, ShortGenerator msgTypeGen)
		{
			if (msgTypeGenerator != null)
				throw new Exception(string.Format("The Scope {0} has already been assigned a MsgType value", GetType()));

			this.scopeIdentifier = scopeIdentifier;
			
			msgType = msgTypeGen.AllocateValue();
			msgTypeGenerator = msgTypeGen;

			// register with the Server's Scope msgType
			NetworkServer.RegisterHandler(msgType, ProcessPeerMessage);
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			msgTypeGenerator.DeallocateValue(msgType);
			msgTypeGenerator = null;

			// attempt to unregister the message
			NetworkServer.UnregisterHandler(msgType);

			// dispose of this to get rid of cycle reference
			Master = null;
		}
		#endregion

		private void SetTargetPeer(IScopePeer targetPeer)
		{
			_targetPeer = (TPeer)targetPeer;
			_targetPeerGroup = null;
			IsTargetGroup = false;
		}

		protected Dictionary<NetworkConnection,TPeer> Peers = new Dictionary<NetworkConnection, TPeer>();

		/// <summary>
		/// Adds the Peer to the Scope, allowing Signal communication with the Client.
		/// NOTE: Sends an EnterScope message to the Peer.
		/// </summary>
		/// <param name="peer">The target Peer.</param>
		public void AddPeer(TPeer peer)
		{
			Peers[peer.connection] = peer;

			ScopeUtils.SendScopeEnteredMessage(this, peer.connection);

			// notify derived class that the peer has entered the scope and is able to receive Signals from the client
			OnPeerEnteredScope(peer);

			// register the RemovePeer method with the peer disconnect event to clean up the scope after a peer disconnects
			Master.OnPeerDisconnected += RemovePeer;
		}

		public void RemovePeer(TPeer peer)
		{
			// we don't need to know when
			Master.OnPeerDisconnected -= RemovePeer;

			// attempt to remove the peer associated with the peer's connection
			if (Peers.Remove(peer.connection))
			{
				ScopeUtils.SendScopeExitedMessage(this, peer.connection);

				// notify derived class that the peer has exited the scope and can no longer receive Signals from the client
				OnPeerExitedScope(peer);
			}
		}

		/// <summary>
		/// Removes the Peer from this Scope and adds them to the specified targetScope.
		/// </summary>
		/// <param name="peer">The Peer.</param>
		/// <param name="targetScope">The Scope to handover the Peer to.</param>
		public void HandoverPeer(TPeer peer, BaseServerScope<TPeer> targetScope)
		{
			// remove peer from this scope
			RemovePeer(peer);

			// add peer to target scope
			targetScope.AddPeer(peer);
		}

		protected virtual void OnPeerEnteredScope(TPeer peer)
		{
		}

		protected virtual void OnPeerExitedScope(TPeer peer)
		{
		}

		public bool IsTargetGroup { get; private set; }

		private TPeer _targetPeer;
		private IEnumerable<TPeer> _targetPeerGroup;

		public TPeer SenderPeer { get; private set; }

		/// <summary>
		/// The desired peer to send the Signal to. By default, it is the sender of the last Signal.
		/// </summary>
		public TPeer TargetPeer
		{
			get { return _targetPeer; }
			set { SetTargetPeer(value); }
		}

		/// <summary>
		/// The desired peer group to send the Signal to.
		/// </summary>
		public IEnumerable<TPeer> TargetPeerGroup
		{
			get { return _targetPeerGroup; }
			set { SetTargetPeerGroup(value); }
		}

		private void SetTargetPeerGroup(IEnumerable<TPeer> targetPeerGroup)
		{
			// set target peer for future outgoing Signal calls
			_targetPeer = default(TPeer);
			_targetPeerGroup = targetPeerGroup;
			IsTargetGroup = true;
		}

		public void ProcessPeerMessage(NetworkMessage msg)
		{
			TPeer peer;

			// ignore messages from unregistered users
			if (!Peers.TryGetValue(msg.conn, out peer))
				return;

			// assign sender peer before calling the incoming Signal method
			SenderPeer = peer;

			// set next outgoing message target to the sender peer
			SetTargetPeer(peer);

			// process the message
			ProcessMessage(msg);
		}

	}
}
