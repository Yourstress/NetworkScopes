
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

		public override void SendRaw (INetworkWriter writer)
		{
			byte[] data = writer.GetBytes();
			client.SendBytes(data, data.Length, 0);
		}

		public override INetworkWriter CreateNetworkWriter ()
		{
			return new UNetNetworkWriter();
		}

		#region Internals
		void SetupClient()
		{
			client = new NetworkClient();

			HostTopology topology = overrideTopology ?? new HostTopology (UnetUtil.CreateConnectionConfig (), 3000);

			client.Configure (topology);

			client.RegisterHandler (UNetMsgType.Connect, UnetOnConnect);
			client.RegisterHandler (UNetMsgType.Disconnect, UnetOnDisconnect);
//			client.RegisterHandler (UNetMsgType.Error, UnetOnError);
//
//			client.RegisterHandler (UNetMsgType.EnterScope, UnetOnEnterScope);
//			client.RegisterHandler (UNetMsgType.ExitScope, UnetOnExitScope);
//			client.RegisterHandler (UNetMsgType.SwitchScope, UnetOnSwitchScope);
//			client.RegisterHandler (UNetMsgType.DisconnectMessage, UnetOnDisconnectMessage);
//			client.RegisterHandler (UNetMsgType.RedirectMessage, UnetOnRedirectMessage);
//			
			client.RegisterHandler (UNetMsgType.ScopeSignal, UnetOnScopeSignal);
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
			OnReceiveRaw(new UNetNetworkReader(msg.reader));
		}
		#endregion
	}
}