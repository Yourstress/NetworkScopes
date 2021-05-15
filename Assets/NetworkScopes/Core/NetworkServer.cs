	
using System;
using System.Collections.Generic;

namespace NetworkScopes
{
	public abstract class NetworkServer<TPeer> : INetworkServer<TPeer> where TPeer : INetworkPeer
	{
		// IServerProvider
		public abstract bool IsListening { get; }
		public abstract bool StartListening(int port);
		public abstract void StopListening();

		// IServerSignalProvider
		public abstract ISignalWriter CreateSignal(short channelId);
		public abstract void SendSignal(PeerTarget target, ISignalWriter writer);

		public event Action<INetworkPeer> OnPeerConnected = delegate { };
		public event Action<INetworkPeer> OnPeerDisconnected = delegate { };

		protected readonly Dictionary<ScopeChannel,IServerScope> registeredScopes = new Dictionary<ScopeChannel, IServerScope>();

		public abstract IReadOnlyCollection<TPeer> Peers { get; }
		public int PeerCount { get; private set; }

		public IServerScope defaultScope { get; set; }

		public int RegisteredScopeCount => registeredScopes.Count;

		private readonly ChannelGenerator channelGenerator = new ChannelGenerator(short.MinValue, short.MaxValue);

		public TServerScope RegisterScope<TServerScope>(byte scopeIdentifier) where TServerScope : IServerScope, new()
		{
			TServerScope newScope = new TServerScope();
			RegisterScope(newScope, scopeIdentifier);
			return newScope;
		}
		
		public TServerScope RegisterScope<TServerScope>(TServerScope scope, byte scopeIdentifier) where TServerScope : IServerScope
		{
			// TODO: instead of writing channel as first short in the packet, use LiteNetLib's channel option when sending
			scope.InitializeServerScope(this, scopeIdentifier, channelGenerator);
		
			registeredScopes[scope.channel] = scope;
			
			if (defaultScope == null)
				defaultScope = scope;

			return scope;
		}

		public void UnregisterScope<TServerScope>(TServerScope scope) where TServerScope : IServerScope
		{
			// don't allow unregistering default scope
			if (defaultScope == (IServerScope)scope)
				throw new Exception("Failed to unregister Scope because it is the default Scope");

			// remove from registered scopes
			if (registeredScopes.Remove(scope.channel))
			{
				// make sure to clean-up before letting go of this scope
				scope.Dispose();
			}
			else
			{
				throw new Exception($"Failed to remove the Scope {scope.GetType()} because it was not registered {GetType().Name}.");
			}
		}

		public TPeer FindPeer(Func<TPeer, bool> peerSelector)
		{
			foreach (TPeer peer in Peers)
			{
				if (peerSelector(peer))
					return peer;
			}

			return default;
		}

		protected void PeerConnected(INetworkPeer peer)
		{
			PeerCount++;
			
			OnPeerConnected(peer);

			if (defaultScope == null)
				throw new Exception("Default scope is not yet set.");

			// add the peer to the default scope
			defaultScope.AddPeer(peer, true);
		}

		protected void PeerDisconnected(INetworkPeer peer)
		{
			// force the peer to disconnect across all scopes, as well clean-up within the Master (Peer_OnDisconnect)
			peer.ForceDisconnect(false);

			PeerCount--;

			OnPeerDisconnected(peer);
		}

		protected void ProcessSignal(ISignalReader signal, INetworkPeer sender)
		{
			ScopeChannel targetChannel = signal.ReadScopeChannel();

			// in order to receive a signal, the receiving scope must be active (got an Entered Scope system message).
			if (registeredScopes.TryGetValue(targetChannel, out IServerScope targetScope))
			{
				targetScope.ProcessSignal(signal, sender);
			}
			else
			{
				NSDebug.LogWarning($"Server could not process signal on unknown channel {targetChannel}.");
			}
		}
	}
}
