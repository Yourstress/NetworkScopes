
namespace NetworkScopes
{
	using System;
	using System.Net.Sockets;
	using System.Collections.Generic;

	#if UNITY_5_3_OR_NEWER
	using System.Threading;
	#endif

	public class SocketClient : BaseClient, IDisposable
	{
		TcpClient tcpClient = null;
		NetworkStream stream;

		bool isInitialized = false;

		Dictionary<short,ScopeHandlerDelegate> socketMsgHandlers = new Dictionary<short, ScopeHandlerDelegate>();

		public SocketClient()
		{
			socketMsgHandlers[ScopeMsgType.ScopeSignal] = SocketOnScopeSignal;
			socketMsgHandlers[ScopeMsgType.EnterScope] = SocketOnEnterScope;
			socketMsgHandlers[ScopeMsgType.ExitScope] = SocketOnExitScope;
			socketMsgHandlers[ScopeMsgType.SwitchScope] = SocketOnSwitchScope;
			socketMsgHandlers[ScopeMsgType.DisconnectMessage] = SocketOnDisconnectMessage;
			socketMsgHandlers[ScopeMsgType.RedirectMessage] = SocketOnRedirectMessage;
		}

		public void Dispose()
		{
			DisconnectInternal();
		}

		// this.hostname and this.port are already assigned to hostname and port before this call
		protected override void ConnectInternal (string hostname, int port)
		{
			if (tcpClient == null)
				SetupClient();
			
			#if UNITY_5_3_OR_NEWER
			ClientSocketDispatcher.TryInitialize();

			ThreadPool.QueueUserWorkItem(RunConnectProcess);
			#endif
		}

		void RunConnectProcess(object state)
		{
			#if UNITY_5_3_OR_NEWER
			try
			{
				tcpClient.Connect(serverHost, serverPort);

				stream = tcpClient.GetStream();

				isInitialized = true;
				ClientSocketDispatcher.QueueAction(OnConnect);

				ThreadPool.QueueUserWorkItem(ReadStream);
				ThreadPool.QueueUserWorkItem(RunVerifyConnection);
			}
			catch
			{
				lastDisconnectMsg = 0;

				ClientSocketDispatcher.QueueAction(OnDisconnect);
			}
			#else
			stream = null;
			throw new System.NotImplementedException();
			#endif
		}

		void RunVerifyConnection(object state)
		{
			while (true)
			{
				#if UNITY_5_3_OR_NEWER
				Thread.Sleep(5000);
				#endif

				bool isConnected = !(tcpClient.Client.Poll(1, SelectMode.SelectRead) && tcpClient.Client.Available == 0);

				if (!isConnected)
				{
					lastDisconnectMsg = 0;

					#if UNITY_5_3_OR_NEWER
					ClientSocketDispatcher.QueueAction(OnDisconnect);
					#else
					OnDisconnect();
					#endif
					return;
				}
			}
		}

		protected override void DisconnectInternal ()
		{
			#if UNITY_5_3_OR_NEWER
			if (tcpClient != null)
				tcpClient.Close();
			#else
			if (tcpClient != null)
				tcpClient.Dispose();
			#endif
			
			isInitialized = false;
		}

		protected override void SetupClient ()
		{
			try
			{
				tcpClient = new TcpClient();
			}
			catch (Exception e)
			{
				ScopeUtils.LogError("Client Socket Exception: {0}", e);
			}
		}

		protected override void ShutdownClient ()
		{
			DestroyClient();
		}

		protected override void DestroyClient ()
		{
			if (tcpClient != null)
			{
				#if UNITY_5_3_OR_NEWER
				tcpClient.Close();
				#else
				tcpClient.Dispose();
				#endif

				tcpClient = null;
			}
		}

		protected override void SendInternal (IMessageWriter writer)
		{
			if (!isInitialized)
				return;

			SocketMessageWriter sw = (SocketMessageWriter)writer;

			sw.FinishMessage();

			byte[] data = sw.stream.ToArray();

			stream.Write(data, 0, data.Length);
		}
		public override IMessageWriter CreateWriter (short scopeChannel, int signalType)
		{
			IMessageWriter writer = new SocketMessageWriter(ScopeMsgType.ScopeSignal, true);
			writer.Write(scopeChannel);
			writer.Write(signalType);
			return writer;
		}
		public override void PrepareAndSendWriter (IMessageWriter writer)
		{
			SendInternal(writer);
		}

		private void ReadStream(object state)
		{
			int readLength;
			byte[] buffer = new byte[4096];

			// each message starts with a length
			while (stream.Read(buffer, 0, sizeof(ushort)) == 2)
			{
				ushort msgLength = BitConverter.ToUInt16(buffer, 0);
				readLength = stream.Read(buffer, 0, msgLength);


				if (readLength == 0)
					break;

				SocketMessageReader reader = new SocketMessageReader(buffer, readLength);
				short msgType = reader.ReadInt16();

				ScopeHandlerDelegate handler;
				if (!socketMsgHandlers.TryGetValue(msgType, out handler))
				{
					ScopeUtils.LogError("Failed to process received message of type " + msgType);
					continue;
				}

				#if UNITY_5_3_OR_NEWER
				ClientSocketDispatcher.AddReceivedMessage(reader, handler);

				Thread.Sleep(10);
				#endif
			}

			ClientSocketDispatcher.QueueAction(OnDisconnect);
		}

		#region Socket Network Messages
		void SocketOnError(IMessageReader msg)
		{
			OnError(msg.ReadString());
		}

		void SocketOnScopeSignal(IMessageReader msg)
		{
			OnScopeSignal(msg);
		}

		void SocketOnEnterScope(IMessageReader msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.ReadInt16();

			// 2. read scopeIdentifier
			byte scopeIdentifier = msg.ReadByte();

			ProcessEnterScope(scopeIdentifier, scopeMsgType);
		}

		void SocketOnExitScope(IMessageReader msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.ReadInt16();

			ProcessExitScope(scopeMsgType);
		}

		void SocketOnSwitchScope(IMessageReader msg)
		{
			// 1. read msgType of prevScope
			short prevScopeMsgType = msg.ReadInt16();

			// 2. read msgType of newScope
			short newScopeMsgType = msg.ReadInt16();

			// 3. read scopeIdentifier of newScope
			byte newScopeIdentifier = msg.ReadByte();

			// simulate exit/enter signals in one go
			ProcessExitScope(prevScopeMsgType);
			ProcessEnterScope(newScopeIdentifier, newScopeMsgType);
		}

		void SocketOnDisconnectMessage(IMessageReader msg)
		{
			lastDisconnectMsg = msg.ReadByte();

			OnDisconnect();
		}

		void SocketOnRedirectMessage(IMessageReader msg)
		{
		}
		#endregion
	}
}