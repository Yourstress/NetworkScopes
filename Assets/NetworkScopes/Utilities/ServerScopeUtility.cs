
namespace NetworkScopes.Utilities
{
	public static class ServerScopeUtility
	{
		public static void SendEnterScopeMessage(INetworkPeer peer, IServerSignalProvider signalProvider, IServerScope scope)
		{
			ISignalWriter writer = signalProvider.CreateSignal(ScopeChannel.EnterScope);
			
			// 1. channel: Determines which channel to communicate on.
			writer.WriteScopeChannel(scope.channel);
			
			// 2. scopeIdentifier: The value which identifier the counterpart client class.
			writer.WriteScopeIdentifier(scope.scopeIdentifier);

			peer.SendSignal(writer);
		}

		public static void SendExitScopeMessage(INetworkPeer peer, IServerSignalProvider signalProvider, IServerScope scope)
		{
			ISignalWriter writer = signalProvider.CreateSignal(ScopeChannel.ExitScope);
			
			// 1. channel: Determines which channel to communicate on.
			writer.WriteScopeChannel(scope.channel);

			peer.SendSignal(writer);
		}
		
		public static void SendSwitcheScopeMessage(INetworkPeer peer, IServerSignalProvider signalProvider, IServerScope prevScope, IServerScope newScope)
		{
			ISignalWriter writer = signalProvider.CreateSignal(ScopeChannel.SwitchScope);
			
			// 1. channel: Specify previous scope channel
			writer.WriteScopeChannel(prevScope.channel);

			// 2. channel: Specify new scope channel
			writer.WriteScopeChannel(newScope.channel);

			// 3. scopeIdentifier: The value which identifies the counterpart (new) client scope
			writer.WriteScopeIdentifier(newScope.scopeIdentifier);
			
			peer.SendSignal(writer);
		}
	}
}