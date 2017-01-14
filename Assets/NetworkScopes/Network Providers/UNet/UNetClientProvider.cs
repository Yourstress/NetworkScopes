
namespace NetworkScopes.UNet
{
	using UnityEngine.Networking;

	public interface IClientCallbacks
	{
		void OnConnect();
	}

	public class UNetClientProvider : BaseClientProvider
	{
		public NetworkClient client { get; private set; }
		public HostTopology overrideTopology = null;

		protected override void ConnectClient (string hostname, int port)
		{
			if (client == null)
				SetupClient();

			client.Connect(hostname, port);
		}

		protected override void DisconnectClient()
		{
			client.Disconnect();
		}

		#region Internals
		void SetupClient()
		{
			client = new NetworkClient();

			HostTopology topology = overrideTopology ?? new HostTopology (UnetUtil.CreateConnectionConfig (), 3000);

			client.Configure (topology);

			client.RegisterHandler (MsgType.Connect, UnetOnConnect);
			client.RegisterHandler (MsgType.Disconnect, UnetOnDisconnect);
//			client.RegisterHandler (UNetMsgType.Error, UnetOnError);
//
//			client.RegisterHandler (UNetMsgType.EnterScope, UnetOnEnterScope);
//			client.RegisterHandler (UNetMsgType.ExitScope, UnetOnExitScope);
//			client.RegisterHandler (UNetMsgType.SwitchScope, UnetOnSwitchScope);
//			client.RegisterHandler (UNetMsgType.DisconnectMessage, UnetOnDisconnectMessage);
//			client.RegisterHandler (UNetMsgType.RedirectMessage, UnetOnRedirectMessage);
//			
			client.RegisterHandler (UnetUtil.ValidateMsgType(NetworkMsgType.ScopeSignal), UnetOnScopeSignal);
		}
		#endregion

		#region UNet Callback Routing
		void UnetOnConnect(NetworkMessage msg)
		{
			OnConnect();
		}

		void UnetOnDisconnect(NetworkMessage msg)
		{
			OnDisconnect();
		}

		void UnetOnScopeSignal(NetworkMessage msg)
		{
			UnityEngine.Debug.Log("Receive");
			OnReceiveRaw(new UNetNetworkReader(msg.reader));
		}
		#endregion
	}
}