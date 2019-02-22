
using Lidgren.Network;


namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;

	public interface IPeerTarget<TPeer> where TPeer : NetworkPeer
	{
		bool IsTargetGroup { get; }
		TPeer TargetPeer { get; set; }
		IEnumerable<TPeer> TargetPeerGroup { get; set; }
	}

	public abstract class BaseServerScope<TPeer> : BaseScope, IPeerTarget<TPeer>, IDisposable where TPeer : NetworkPeer, new()
	{
		public MasterServer<TPeer> Master { get; private set; }

		public readonly List<TPeer> Peers = new List<TPeer>();

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
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			OnDispose();
			
			msgTypeGenerator.DeallocateValue(msgType);
			msgTypeGenerator = null;

			// dispose of this to get rid of cycle reference
			Master = null;
		}

		protected virtual void OnDispose()
		{

		}
		#endregion

		/// <summary>
		/// Adds the Peer to the Scope, allowing Signal communication with the Client.
		/// NOTE: Sends an EnterScope message to the Peer.
		/// </summary>
		/// <param name="peer">The target Peer.</param>
		public virtual void AddPeer(TPeer peer, bool sendEnterMsg = true)
		{
//			NetworkDebug.LogFormat("<color=cyan>ADDING {0}</color> to {1}", peer, GetType().Name);
			Peers.Add(peer);

			// register for disconnection to clean up after this peer
			peer.OnDisconnect += OnPeerDisconnected;

			if (sendEnterMsg)
				ScopeUtils.SendScopeEnteredMessage(this, peer);

			// notify derived class that the peer has entered the scope and is able to receive Signals from the client
			OnPeerEnteredScope(peer);
		}

		public virtual void RemovePeer(TPeer peer, bool sendExitMsg = true)
		{
			// we don't care about d/c events from this peer anymore
			peer.OnDisconnect -= OnPeerDisconnected;

			// attempt to remove the peer associated with the peer's connection
			if (Peers.Remove(peer))
			{
				if (sendExitMsg && peer.isConnected)
					ScopeUtils.SendScopeExitedMessage(this, peer);

				// notify derived class that the peer has exited the scope and can no longer receive Signals from the client
				OnPeerExitedScope(peer);
			}
			else
			{
				NetworkDebug.LogFormat("Failed to remove non-existent peer {0} from scope {1}", peer, GetType().Name);
			}
		}
			
		protected virtual void OnPeerDisconnected (NetworkPeer peer)
		{
			RemovePeer((TPeer)peer, peer.sendExitScopeMsgOnDisconnect);
		}

		/// <summary>
		/// Removes the Peer from this Scope and adds them to the specified targetScope.
		/// </summary>
		/// <param name="peer">The Peer.</param>
		/// <param name="targetScope">The Scope to handover the Peer to.</param>
		public void HandoverPeer(TPeer peer, BaseServerScope<TPeer> targetScope)
		{
			// tell the peer about this handover
			ScopeUtils.SendScopeSwitchedMessage(this, targetScope, peer);

			// remove peer from this scope
			RemovePeer(peer, false);

			// add peer to target scope
			targetScope.AddPeer(peer, false);
		}
		
		public void RedirectPeer(TPeer peer, string hostname, int port)
		{
			Master.RedirectPeer(peer, hostname, port);
		}

		public void RedirectPeers(IEnumerable<TPeer> peers, string hostname, int port)
		{
			foreach (TPeer peer in peers)
				peer.Redirect(hostname, port);
		}

		protected virtual void OnPeerEnteredScope(TPeer peer)
		{
		}

		protected virtual void OnPeerExitedScope(TPeer peer)
		{
		}
		
		public TPeer SenderPeer { get; private set; }

		public bool IsTargetGroup { get; private set; }

		private TPeer _targetPeer;
		private IEnumerable<TPeer> _targetPeerGroup;

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

		private void SetTargetPeer(TPeer targetPeer)
		{
			_targetPeer = targetPeer;
			_targetPeerGroup = null;
			IsTargetGroup = false;
		}

		private void SetTargetPeerGroup(IEnumerable<TPeer> targetPeerGroup)
		{
			_targetPeer = default(TPeer);
			_targetPeerGroup = targetPeerGroup;
			IsTargetGroup = true;
		}

		public void ProcessPeerSignal(NetIncomingMessage msg, TPeer sender)
		{
			// ignore messages from unregistered users
			if (!Peers.Contains(sender))
				return;
			
			// set the sender peer
			SenderPeer = sender;
			
			// set the next signal target to this peer by default (reply-style)
			SetTargetPeer(sender);

			// process the message
			ProcessSignal(msg);
		}
	}
}
