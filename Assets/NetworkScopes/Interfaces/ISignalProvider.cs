
using System;
using System.Collections.Generic;

namespace NetworkScopes
{
	public interface INetworkServer<out TPeer> : INetworkServer where TPeer : INetworkPeer
	{
		IReadOnlyCollection<TPeer> Peers { get; }
		TPeer FindPeer(Func<TPeer, bool> peerSelector);
	}

	public interface INetworkServer : IServerProvider, IServerScopeProvider
	{
		IServerScope defaultScope { get; set; }
		
		int RegisteredScopeCount { get; }
	}
	public interface INetworkClient : IClientProvider, IScopeRegistrar
	{ }
	
	public interface ISignalProvider
	{
		ISignalWriter CreateSignal(short channelId);
	}

	public interface IClientSignalProvider : ISignalProvider
	{
		void SendSignal(ISignalWriter signal);
	}

	public interface IServerSignalProvider : ISignalProvider
	{
		void SendSignal(PeerTarget target, ISignalWriter writer);
	}

	public interface IScopeRegistrar
	{
		TServerScope RegisterScope<TServerScope>(byte scopeIdentifier) where TServerScope : IServerScope, new();
		TServerScope RegisterScope<TServerScope>(TServerScope newScope, byte scopeIdentifier) where TServerScope : IServerScope;
		void UnregisterScope<TServerScope>(TServerScope scope) where TServerScope : IServerScope;
	}

	public interface IServerScopeProvider : IScopeRegistrar, IServerSignalProvider
	{
	}
}