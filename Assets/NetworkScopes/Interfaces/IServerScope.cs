namespace NetworkScopes
{
	public interface IServerScope : IBaseScope
	{
		ScopeIdentifier scopeIdentifier { get; }
		ScopeChannel channel { get; }

		void InitializeServerScope(IServerScopeProvider scopeProvider, ScopeIdentifier serverScopeIdentifier, ChannelGenerator channelGenerator);

		void AddPeer(INetworkPeer peer);
		void RemovePeer(INetworkPeer peer);

		void ProcessSignal(ISignalReader signal, INetworkPeer sender);
	}
}