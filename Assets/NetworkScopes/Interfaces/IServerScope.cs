namespace NetworkScopes
{
	public interface IServerScope : IBaseScope
	{
		ScopeIdentifier scopeIdentifier { get; }
		ScopeChannel currentChannel { get; }

		void InitializeServerScope(IServerSignalProvider signalProvider, ScopeIdentifier scopeIdentifier, ScopeChannel scopeChannel);
		void AddPeer(INetworkPeer peer);
		void RemovePeer(INetworkPeer peer);
	}
}