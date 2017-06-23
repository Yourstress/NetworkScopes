namespace NetworkScopes
{
	public interface IServerScope : IBaseScope
	{
		ScopeIdentifier scopeIdentifier { get; }
		ScopeChannel currentChannel { get; }

		void InitializeServerScope(IServerScopeProvider scopeProvider, ScopeIdentifier scopeIdentifier, ScopeChannel scopeChannel);

		void AddPeer(INetworkPeer peer);
		void RemovePeer(INetworkPeer peer);

		void ProcessSignal(ISignalReader signal, INetworkPeer sender);
	}
}