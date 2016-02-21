
namespace NetworkScopes
{
	using System;
	using UnityEngine.Networking;
	using System.Collections.Generic;
	using UnityEngine;

	public abstract class MasterServer<TPeer> where TPeer : IScopePeer
	{
		protected Dictionary<NetworkConnection,TPeer> peers = new Dictionary<NetworkConnection, TPeer>();

		public int NumberOfPeers { get { return peers.Count; } }

		public event Action<TPeer> OnPeerConnected = delegate {};
		public event Action<TPeer> OnPeerDisconnected = delegate {};

		public TPeer GetPeer(NetworkConnection connection)
		{
			return peers[connection];
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
			ConnectionConfig conConfig = new ConnectionConfig();
			conConfig.AddChannel(QosType.ReliableSequenced);
			HostTopology topology = new HostTopology(conConfig, 3000);
			NetworkServer.Configure(topology);

			NetworkServer.Listen(port);
			NetworkServer.RegisterHandler(MsgType.Connect, OnPeerConnectedMsg);
			NetworkServer.RegisterHandler(MsgType.Disconnect, OnPeerDisconnectedMsg);
		}

		void OnPeerConnectedMsg(NetworkMessage msg)
		{
			// create the peer
			TPeer peer = CreatePeer(msg.conn);

			// add to peers
			peers[msg.conn] = peer;

			// notify event listeners
			OnPeerConnected(peer);

			// add this peer to the default scope
			defaultScope.AddPeer(peer);
		}

		void OnPeerDisconnectedMsg(NetworkMessage msg)
		{
			// get peer
			TPeer peer = peers[msg.conn];

			// perform any required clean-up
			DestroyPeer(peer);

			// remove from collection
			peers.Remove(msg.conn);

			// notify event listeners
			OnPeerDisconnected(peer);
		}

		protected abstract TPeer CreatePeer(NetworkConnection connection);
		protected abstract void DestroyPeer(TPeer peer);
	}
}