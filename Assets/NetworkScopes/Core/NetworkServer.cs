
using System;
using System.Collections.Generic;
using NetworkScopes.ServiceProviders.Lidgren;
using UnityEngine;

namespace NetworkScopes
{
	public abstract class NetworkServer : IServerProvider, IServerSignalProvider
	{
		// IServerProvider
		public abstract bool IsListening { get; }
		public abstract bool StartListening(int port);
		public abstract void StopListening();

		// IServerSignalProvider
		public abstract ISignalWriter CreateSignal(short scopeIdentifier);
		public abstract void SendSignal(PeerTarget target, ISignalWriter writer);

		public event Action<INetworkPeer> OnPeerConnected = delegate { };
		public event Action<INetworkPeer> OnPeerDisconnected = delegate { };

		protected readonly List<IServerScope> registeredScopes = new List<IServerScope>();

		public IServerScope defaultScope;

		public TServerScope RegisterScope<TServerScope>(byte scopeIdentifier) where TServerScope : IServerScope, new()
		{
			TServerScope newScope = new TServerScope();

			if (defaultScope == null)
				defaultScope = newScope;

			// TODO: create channel for each registered scope -- channel hard coded to 0!!
			newScope.InitializeServerScope(this, scopeIdentifier, 0);

			registeredScopes.Add(newScope);

			return newScope;
		}

		public void UnregisterScope<TServerScope>(TServerScope scope) where TServerScope : IServerScope
		{
			if (!registeredScopes.Remove(scope))
				throw new Exception(string.Format("The scope {0} is not registered.", scope));
		}

		protected void PeerConnected(INetworkPeer peer)
		{
			OnPeerConnected(peer);

			if (defaultScope == null)
				throw new Exception("Default scope is not yet set.");

			// add the peer to the default scope
			defaultScope.AddPeer(peer);
		}

		protected void PeerDisconnected(INetworkPeer peer)
		{
			OnPeerDisconnected(peer);
		}

		// Static server factory methods
		public static LidgrenServer CreateLidgrenServer()
		{
			return new LidgrenServer();
		}
	}
}
