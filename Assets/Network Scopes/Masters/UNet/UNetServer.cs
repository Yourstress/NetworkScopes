
#if UNITY_5_3_OR_NEWER
namespace NetworkScopesV2
{
	using UnityEngine.Networking;
	using System.Collections.Generic;

	public abstract class UNetServer<TPeer> : BaseServer<TPeer> where TPeer : UnetNetworkPeer, new()
	{
		Dictionary<NetworkConnection,TPeer> connectionPeers = new Dictionary<NetworkConnection, TPeer> ();

		protected override void StartServer ()
		{
			HostTopology topology = new HostTopology (UnetUtil.CreateConnectionConfig (), 3000);
			NetworkServer.Configure (topology);

			NetworkServer.Listen (listenPort);

			NetworkServer.RegisterHandler (MsgType.Connect, UnetOnConnectMsg);
			NetworkServer.RegisterHandler (MsgType.Disconnect, UnetOnDisconnectMsg);
			NetworkServer.RegisterHandler (ScopeMsgType.ScopeSignal, UnetOnScopeSignal);
		}

		public override void StopServer ()
		{
			NetworkServer.Reset ();
		}

		public override IMessageWriter CreateWriter (short msgType)
		{
			return new UnetMessageWriter (msgType);
		}

		public override void SendWriter (IMessageWriter writer, TPeer targetPeer)
		{
			targetPeer.Send (writer);
		}

		public override void SendWriter (IMessageWriter writer, IEnumerable<TPeer> targetPeers)
		{
			byte error;

			NetworkWriter networkWriter = ((UnetMessageWriter)writer).writer;
			networkWriter.FinishMessage ();

			short bufferSize = networkWriter.Position;
			byte[] bufferData = networkWriter.ToArray ();

			foreach (TPeer peer in targetPeers) {
				if (!peer.isConnected)
					return;

				NetworkTransport.Send (peer.connection.hostId, peer.connection.connectionId, 0, bufferData, bufferSize, out error);

				#if SHOW_SEND_ERRORS
				NetworkError nerror = (NetworkError)error;
				if (nerror != NetworkError.Ok)
				Debug.LogError("Network error: " + nerror);
				#endif
			}
		}

		void UnetOnConnectMsg (NetworkMessage msg)
		{
			TPeer peer = new TPeer ();
			peer.Initialize (msg.conn);

			connectionPeers [msg.conn] = peer;

			PeerConnected (peer);
		}

		void UnetOnDisconnectMsg (NetworkMessage msg)
		{
			TPeer peer = connectionPeers [msg.conn];
			PeerDisconnected (peer);
		}

		void UnetOnScopeSignal (NetworkMessage msg)
		{
			PeerSentSignal (new UnetMessageReader (msg.reader), connectionPeers [msg.conn]);
		}

		protected override void Peer_OnDisconnect (NetworkPeer netPeer)
		{
			UnetNetworkPeer unetPeer = (UnetNetworkPeer)netPeer;

			connectionPeers.Remove (unetPeer.connection);
			
			base.Peer_OnDisconnect (netPeer);
		}
	}
}
#endif