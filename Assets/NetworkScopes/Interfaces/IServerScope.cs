using System;

namespace NetworkScopes
{
	public interface IServerScope : IBaseScope, IDisposable
	{
		ScopeIdentifier scopeIdentifier { get; }
		ScopeChannel channel { get; }
		
		IServerScope fallbackScope { get; }

		void InitializeServerScope(INetworkServer networkServer, ScopeIdentifier serverScopeIdentifier, ChannelGenerator channelGenerator);

		void AddPeer(INetworkPeer peer, bool sendEnterMsg);
		void RemovePeer(INetworkPeer peer, bool sendExitMsg);

		void HandoverPeer(INetworkPeer peer, IServerScope targetScope);

		void ProcessSignal(ISignalReader signal, INetworkPeer sender);
	}
}