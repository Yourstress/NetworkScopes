
namespace NetworkScopes.UNet
{
	using UnityEngine.Networking;

	public class NetworkMsgType
	{
		// server to client messages
		public const short Authenticate = 0;
		public const short EnterScope = 1;
		public const short ExitScope = 2;
		public const short SwitchScope = 3;
		public const short DisconnectMessage = 4;
		public const short RedirectMessage = 5;

		// two-way messages
		public const short ScopeSignal = 10;
	}
}