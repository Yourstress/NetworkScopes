
namespace NetworkScopes.UNet
{
	using UnityEngine.Networking;

	public class UNetMsgType
	{
		// connection-state messages
		public const short Connect = MsgType.Connect;
		public const short Disconnect = MsgType.Disconnect;
		public const short Error = MsgType.Error;

		// server to client messages
		public const short EnterScope = 90;
		public const short ExitScope = 91;
		public const short SwitchScope = 92;
		public const short DisconnectMessage = 93;
		public const short RedirectMessage = 94;

		// two-way messages
		public const short ScopeSignal = 91;
	}
}