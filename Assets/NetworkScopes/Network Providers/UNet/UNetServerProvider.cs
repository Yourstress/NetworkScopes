
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

			NetworkServer.RegisterHandler(UNetMsgType.Connect, UnetOnConnect);
			NetworkServer.RegisterHandler(UNetMsgType.Disconnect, UnetOnDisconnect);
			NetworkServer.RegisterHandler(UNetMsgType.ScopeSignal, UnetOnScopeSignal);

		}
		public override void StopServer ()
		{
			NetworkServer.Shutdown();
			NetworkServer.Reset();
		}
		#endregion

		public Dictionary<NetworkConnection,PeerEntity> peers;

		void UnetOnConnect(NetworkMessage msg)
		{
			PeerEntity peer = new PeerEntity();
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
	}
}