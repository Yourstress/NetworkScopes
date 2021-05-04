
namespace NetworkScopes.Utilities
{
	public class SystemMessage
	{
		/// <summary>
		/// Parameters: ScopeIdentifier, ScopeChannel
		/// </summary>
		public const byte EnterScope = 0;
		/// <summary>
		/// Parameters: ScopeChannel
		/// </summary>
		public const byte ExitScope = 1;
	}

	public static class ServerScopeUtility
	{
		public static void SendEnterScopeMessage(INetworkPeer peer, IServerSignalProvider signalProvider, IServerScope scope)
		{
			ISignalWriter signalWriter = signalProvider.CreateSignal(ScopeChannel.SystemChannel);

			signalWriter.Write(SystemMessage.EnterScope);
			signalWriter.WriteScopeIdentifier(scope.scopeIdentifier);
			signalWriter.WriteScopeChannel(scope.currentChannel);

			peer.SendSignal(signalWriter);
		}

		public static void SendExitScopeMessage(INetworkPeer peer, IServerSignalProvider signalProvider, IServerScope scope)
		{
			ISignalWriter signalWriter = signalProvider.CreateSignal(ScopeChannel.SystemChannel);

			signalWriter.Write(SystemMessage.ExitScope);
			signalWriter.Write(scope.currentChannel);

			peer.SendSignal(signalWriter);
		}
	}
}