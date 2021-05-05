	
using System;
using System.Collections.Generic;
using NetworkScopes.ServiceProviders;
using NetworkScopes.ServiceProviders.LiteNetLib;

namespace NetworkScopes
{
	public abstract class NetworkServer<TPeer> : INetworkServer where TPeer : INetworkPeer
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

		protected readonly Dictionary<ScopeChannel,IServerScope> registeredScopes = new Dictionary<ScopeChannel, IServerScope>();

		public abstract IReadOnlyCollection<TPeer> Peers { get; }
		public int PeerCount { get; private set; }

		public IServerScope defaultScope;

		private ShortGenerator channelGenerator = new ShortGenerator(short.MinValue, short.MaxValue);

		public NetworkServer()
		{
			// the channel generator should never generate the system channel
			channelGenerator.AllocateManualValue(ScopeChannel.SystemChannel);
		}

		public TServerScope RegisterScope<TServerScope>(byte scopeIdentifier) where TServerScope : IServerScope, new()
		{
			TServerScope newScope = new TServerScope();
			RegisterScope(newScope, scopeIdentifier);
			return newScope;
		}

		public TServerScope RegisterScope<TServerScope>(TServerScope scope, byte scopeIdentifier) where TServerScope : IServerScope
		{
			ScopeChannel channel = channelGenerator.AllocateValue();

			// TODO: create channel for each registered scope -- channel hard coded to 0!!
			scope.InitializeServerScope(this, scopeIdentifier, channel);

			registeredScopes[channel] = scope;
			
			if (defaultScope == null)
				defaultScope = scope;

			return scope;
		}

		public void UnregisterScope<TServerScope>(TServerScope scope) where TServerScope : IServerScope
		{
			channelGenerator.DeallocateValue(scope.currentChannel);

			if (!registeredScopes.Remove(scope.currentChannel))
				throw new Exception(string.Format("The scope {0} is not registered.", scope));
		}

		protected void PeerConnected(INetworkPeer peer)
		{
			PeerCount++;
			
			OnPeerConnected(peer);

			if (defaultScope == null)
				throw new Exception("Default scope is not yet set.");

			// add the peer to the default scope
			defaultScope.AddPeer(peer);
		}

		protected void PeerDisconnected(INetworkPeer peer)
		{
			peer.TriggerDisconnectEvent();

			PeerCount--;

			OnPeerDisconnected(peer);
		}

		protected void ProcessSignal(ISignalReader signal, INetworkPeer sender)
		{
			ScopeChannel targetChannel = signal.ReadScopeChannel();
			IServerScope targetScope;

			// in order to receive a signal, the receiving scope must be active (got an Entered Scope system message).
			if (registeredScopes.TryGetValue(targetChannel, out targetScope))
			{
				targetScope.ProcessSignal(signal, sender);
			}
			else
			{
				Debug.LogWarning($"Server could not process signal on unknown channel {targetChannel}.");
			}
		}
	}
}
