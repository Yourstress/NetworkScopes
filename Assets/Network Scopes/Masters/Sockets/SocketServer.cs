
namespace NetworkScopes
{
	using System;
	using System.Threading;
	using System.Net;
	using System.Net.Sockets;
	using System.Collections.Generic;

	#if !UNITY_5_3_OR_NEWER
	using System.Threading.Tasks;
	#endif

	public abstract class SocketServer<TPeer> : BaseServer<TPeer> where TPeer : SocketNetworkPeer, new()
	{
		TcpListener server = null;

		Dictionary<TcpClient,TPeer> connectionPeers = new Dictionary<TcpClient,TPeer>();

		Dictionary<short,Action<IMessageReader,TcpClient>> socketMsgHandlers = new Dictionary<short, Action<IMessageReader,TcpClient>>();

		protected override void StartServer ()
		{
			// initialize handlers of different msg types (connect, disconnect, signal)
			socketMsgHandlers[1] = SocketOnConnectMsg;
			socketMsgHandlers[2] = SocketOnDisconnectMsg;
			socketMsgHandlers[ScopeMsgType.ScopeSignal] = SocketOnScopeSignal;

			#if UNITY_5_3_OR_NEWER
			ServerSocketDispatcher.TryInitialize();
			ThreadPool.QueueUserWorkItem(RunServerLoop);
			#else
			Task t = new Task(RunServerLoop);
			t.Start();
			t.Wait();
			// Task t = new Task(RunServerLoop);
			// t.Start();
			// t.Wait();
			// Task.Run((Action)RunServerLoop);
			#endif
		}

		public override void StopServer ()
		{
			foreach (TcpClient client in connectionPeers.Keys)
			{
				#if UNITY_5_3_OR_NEWER
				client.Close();
				#else
				client.Dispose();
				#endif
			}
			connectionPeers.Clear();
			server.Stop();
			server = null;
		}

		#if UNITY_5_3_OR_NEWER
		private void RunServerLoop(object state)
		#else
		private void RunServerLoop()
		#endif
		{
			try
			{
				// initialize and start the server
				server = new TcpListener(IPAddress.Any, listenPort);
				server.Start();

				#if UNITY_5_3_OR_NEWER
				ThreadPool.QueueUserWorkItem((st) => PingPeers());
				#else
				Task pingTask = new Task(PingPeers);
				pingTask.Start();
				#endif

				#if !UNITY_5_3_OR_NEWER
				ScopeUtils.Log($"Server started successfully on port {listenPort}.");
				#endif

				while (true)
				{
					#if UNITY_5_3_OR_NEWER
					// perform a blocking call to accept connection requests
					TcpClient client = server.AcceptTcpClient();

					ServerSocketDispatcher.EnqueueMessage(null, client, SocketOnConnectMsg);
					#else

					TcpClient client = server.AcceptTcpClientAsync().Result;

					SocketOnConnectMsg(null, client);
					#endif
				}
			}
			catch
			{
			}
			finally
			{
				server.Stop();
			}
		}

		private void PingPeers()
		{
			while (true)
			{
				for (int x = peers.Count-1; x >= 0; x--)
				{
					Socket socket = peers[x].connection.Client;

					bool isConnected = !((socket.Poll(1000, SelectMode.SelectRead) && (socket.Available == 0)) || !socket.Connected);

					if (!isConnected)
						peers[x].ForceDisconnect(true);
				}

				#if UNITY_5_3_OR_NEWER
				Thread.Sleep(5000);
				#else
				Task.Delay(5000).Wait();
				#endif
			}
		}

		private void RunClientLoop(object clientObj)
		{
			TPeer peer = (TPeer)clientObj;

			NetworkStream stream = peer.connection.GetStream();
			byte[] buffer = new byte[4096];

			int readLength;

			// each message starts with a length
			try
			{
				while (stream.Read(buffer, 0, sizeof(ushort)) == 2)
				{
					ushort msgLength = BitConverter.ToUInt16(buffer, 0);
					readLength = stream.Read(buffer, 0, msgLength);
					
					if (readLength == 0)
						break;
					
					SocketMessageReader reader = new SocketMessageReader(buffer, readLength);
					
					// get the msg type (connect, disconnect, signal)
					short msgType = reader.ReadInt16();
					
					Action<IMessageReader,TcpClient> msgHandler = socketMsgHandlers[msgType];
					
					#if UNITY_5_3_OR_NEWER
					ServerSocketDispatcher.EnqueueMessage(reader, peer.connection, msgHandler);
					
					Thread.Sleep(10);
					#else
					msgHandler(reader, peer.connection);
					#endif
				}
			}
			catch
			{
			}


			peer.ForceDisconnect(true);
		}

		public override IMessageWriter CreateWriter(short msgType)
		{
			return new SocketMessageWriter(msgType);
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

		void SocketOnConnectMsg(IMessageReader msg, TcpClient client)
		{
			TPeer peer = new TPeer();
			peer.Initialize(client);

			connectionPeers[client] = peer;

			PeerConnected(connectionPeers[client]);

			// start a new thread for this client
			ThreadPool.QueueUserWorkItem(RunClientLoop, peer);
		}

		void SocketOnDisconnectMsg(IMessageReader msg, TcpClient client)
		{
			TPeer peer = connectionPeers[client];
			PeerDisconnected(peer);
		}

		void SocketOnScopeSignal(IMessageReader msg, TcpClient client)
		{
			PeerSentSignal(msg, connectionPeers[client]);
		}

		protected override void Peer_OnDisconnect (NetworkPeer netPeer)
		{
			SocketNetworkPeer sockPeer = (SocketNetworkPeer)netPeer;

			connectionPeers.Remove(sockPeer.connection);

			base.Peer_OnDisconnect (netPeer);
		}
	}
}