
namespace NetworkScopes
{
	using WebSocketSharp;

	public class WebSocketClient : BaseClient
	{
		private WebSocket client;

		#region implemented abstract members of BaseClient
		protected override void ConnectInternal (string hostname, int port)
		{
			ShutdownClient();

			client = new WebSocket(string.Format("ws://{0}:{1}", hostname, port));
			client.OnOpen += Client_OnOpen;
			client.OnClose += Client_OnClose;
			client.OnMessage += Client_OnMessage;
			client.OnError += Client_OnError;

			WebSocketClientDispatcher.Initialize(OnWebSocketEvent);

			client.ConnectAsync();
		}

		protected override void DisconnectInternal ()
		{
			client.CloseAsync();
		}

		protected override void SetupClient ()
		{
		}
		protected override void ShutdownClient ()
		{
			if (client != null)
				client.CloseAsync();
		}
		protected override void DestroyClient ()
		{
			client = null;
		}
		protected override void SendInternal (IMessageWriter writer)
		{
			throw new System.NotImplementedException ();
		}
		public override IMessageWriter CreateWriter (short msgType, int signalType)
		{
			throw new System.NotImplementedException ();
		}
		public override void PrepareAndSendWriter (IMessageWriter writer)
		{
			throw new System.NotImplementedException ();
		}
		#endregion

		void Client_OnOpen (object sender, System.EventArgs e)
		{
			WebSocketClientDispatcher.Enqueue(WSEventType.Open);
		}

		void Client_OnMessage (object sender, MessageEventArgs e)
		{
			WebSocketClientDispatcher.Enqueue(WSEventType.Message, e.RawData);
		}

		void Client_OnClose (object sender, CloseEventArgs e)
		{
			WebSocketClientDispatcher.Enqueue(WSEventType.Close);
		}

		void Client_OnError (object sender, ErrorEventArgs e)
		{
			WebSocketClientDispatcher.Enqueue(WSEventType.Message, System.Text.Encoding.UTF8.GetBytes(e.Message));
		}
			
		void OnWebSocketEvent(WSEventType type, byte[] data)
		{
			switch (type)
			{
			case WSEventType.Open:
				OnConnect();
				break;

			case WSEventType.Close:
				OnDisconnect();
				break;

			case WSEventType.Error:
				OnError(System.Text.Encoding.UTF8.GetString(data));
				break;

			case WSEventType.Message:
				ProcessMessage(new SocketMessageReader(data, data.Length));
				break;
			}
		}

		void ProcessMessage(IMessageReader reader)
		{
			short msgType = reader.ReadInt16();

			switch (msgType)
			{
			case ScopeMsgType.ScopeSignal:
				OnScopeSignal(reader);
				break;

			case ScopeMsgType.EnterScope:
				{
					// 1. read msgType
					short scopeMsgType = reader.ReadInt16();

					// 2. read scopeIdentifier
					byte scopeIdentifier = reader.ReadByte();

					ProcessEnterScope(scopeIdentifier, scopeMsgType);
					break;
				}
			case ScopeMsgType.ExitScope:
				{
					// 1. read msgType
					short scopeMsgType = reader.ReadInt16();

					ProcessExitScope(scopeMsgType);
					break;
				}
			case ScopeMsgType.SwitchScope:
				{
					// 1. read msgType of prevScope
					short prevScopeMsgType = reader.ReadInt16();

					// 2. read msgType of newScope
					short newScopeMsgType = reader.ReadInt16();

					// 3. read scopeIdentifier of newScope
					byte newScopeIdentifier = reader.ReadByte();

					// simulate exit/enter signals in one go
					ProcessExitScope(prevScopeMsgType);
					ProcessEnterScope(newScopeIdentifier, newScopeMsgType);
					break;
				}
			case ScopeMsgType.DisconnectMessage:
				{
					lastDisconnectMsg = reader.ReadByte();
					OnDisconnect();
					break;
				}
			default:
				UnityEngine.Debug.Log("Unhandled msg " + msgType);
				break;
			}
		}
	}
}