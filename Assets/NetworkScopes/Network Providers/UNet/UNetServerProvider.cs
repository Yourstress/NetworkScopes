
namespace NetworkScopes.UNet
{
	using System;
	using System.Collections.Generic;
	using UnityEngine.Networking;

	public class UNetServerProvider : BaseServerProvider
	{
		public HostTopology overrideTopology = null;
		public int maxDefaultConnections = 1000;

		#region implemented abstract members of BaseServerProvider
		public override void StartServer ()
		{
			if (peers == null)
				peers = new Dictionary<NetworkConnection, PeerEntity>(maxDefaultConnections);
			
			HostTopology topology = overrideTopology ?? new HostTopology(UnetUtil.CreateConnectionConfig(), maxDefaultConnections);
			NetworkServer.Configure(topology);

			NetworkServer.Listen(listenPort);

			NetworkServer.RegisterHandler(MsgType.Connect, UnetOnConnect);
			NetworkServer.RegisterHandler(MsgType.Disconnect, UnetOnDisconnect);
			NetworkServer.RegisterHandler(UnetUtil.ValidateMsgType(NetworkMsgType.ScopeSignal), UnetOnScopeSignal);

		}
		public override void StopServer ()
		{
			NetworkServer.Shutdown();
			NetworkServer.Reset();
		}
		#endregion

		public Dictionary<NetworkConnection,PeerEntity> peers;

		#region UNet Callback Routing
		void UnetOnConnect(NetworkMessage msg)
		{
			PeerEntity peer = new PeerEntity(this);
			peers[msg.conn] = peer;

			serverCallbacks.OnConnected(peer);
		}

		void UnetOnDisconnect(NetworkMessage msg)
		{
			PeerEntity peer = peers[msg.conn];
			serverCallbacks.OnDisconnected(peer);

			serverCallbacks.OnDisconnected(peer);
		}

		void UnetOnScopeSignal(NetworkMessage msg)
		{
		}
		#endregion
	}
}