namespace NetworkScopes
{
	public interface IServerScope : IBaseScope
	{
		ScopeIdentifier scopeIdentifier { get; }
		ScopeChannel channel { get; }

		void InitializeServerScope(IServerScopeProvider scopeProvider, ScopeIdentifier serverScopeIdentifier, ChannelGenerator channelGenerator);

		void AddPeer(INetworkPeer peer, bool sendEnterMsg);
		void RemovePeer(INetworkPeer peer, bool sendExitMsg);

		void ProcessSignal(ISignalReader signal, INetworkPeer sender);
	}
}