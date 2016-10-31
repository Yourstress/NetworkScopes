
namespace NetworkScopes
{
	using WebSocketSharp.Server;
	using System.Collections.Generic;

	public abstract class WebSocketServer<TPeer> : BaseServer<TPeer> where TPeer : WebSocketPeer, new()
	{
		private WebSocketServer server = null;

		private Dictionary<WebSocketConnectionHandler,TPeer> indexedPeers = new Dictionary<WebSocketConnectionHandler, TPeer>();

		#region implemented abstract members of BaseServer
		public override IMessageWriter CreateWriter (short msgType)
		{
			return new SocketMessageWriter(msgType, false);
		}
		public override void SendWriter (IMessageWriter writer, TPeer targetPeer)
		{
			targetPeer.Send(writer);
		}

		public override void SendWriter (IMessageWriter writer, IEnumerable<TPeer> targetPeers)
		{
			foreach (TPeer peer in targetPeers)
			{
				if (!peer.isConnected)
					return;

				peer.Send(writer);
			}
		}
		protected override void StartServer ()
		{
			if (server != null)
				throw new System.Exception("WebSocket already started.");
				
			server = new WebSocketServer(listenPort);
			server.AddWebSocketService<WebSocketConnectionHandler>("/");
			server.Start();

			// listen to events queued by the WebSocketConnectionHandler class by registering the server dispatcher callback
			WebSocketServerDispatcher.Initialize(OnWebSocketEvent);
		}
		public override void StopServer ()
		{
			server.Stop();
			server = null;
		}
		#endregion

		void OnWebSocketEvent(WSEvent ev)
		{
			switch (ev.type)
			{
			case WSEventType.Open:
				{
					TPeer peer = new TPeer();
					peer.Initialize(ev.connection);
					indexedPeers[ev.connection] = peer;

					PeerConnected(peer);
					break;
				}

			case WSEventType.Close:
				{
					TPeer peer = indexedPeers[ev.connection];
					indexedPeers.Remove(ev.connection);

					PeerDisconnected(peer);
					break;
				}

			case WSEventType.Error:
					break;

			case WSEventType.Message:
				{
					TPeer peer = indexedPeers[ev.connection];

					IMessageReader reader = new SocketMessageReader(ev.data, ev.data.Length);
					PeerSentSignal(reader, peer);
					break;
				}
			}
		}
	}
}