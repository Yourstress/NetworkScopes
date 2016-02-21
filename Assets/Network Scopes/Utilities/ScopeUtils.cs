
namespace NetworkScopes
{
	using UnityEngine.Networking;

	public static class ScopeUtils
	{
		public static void SendScopeEnteredMessage<TPeer>(BaseServerScope<TPeer> scope, NetworkConnection connection) where TPeer : IScopePeer
		{
			NetworkWriter writer = new NetworkWriter();

			writer.StartMessage(ScopeMsgType.EnterScope);

			// 1. scopeIdentifier: The value which identifier the counterpart client class
			writer.Write(scope.msgType);

			// 2. msgType: Determines which channel to communicate on
			writer.Write(scope.scopeIdentifier);

			writer.FinishMessage();

			connection.SendWriter(writer, 0);
		}

		public static void SendScopeExitedMessage<TPeer>(BaseServerScope<TPeer> scope, NetworkConnection connection) where TPeer : IScopePeer
		{
			NetworkWriter writer = new NetworkWriter();

			writer.StartMessage(ScopeMsgType.ExitScope);

			// 1. msgType: Determines which channel to communicate on
			writer.Write(scope.msgType);

			writer.FinishMessage();

			connection.SendWriter(writer, 0);
		}
	}
}