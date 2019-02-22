using NetworkScopes.Utilities;


namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using Lidgren.Network;

	public abstract class MasterClient
	{
		private NetClient _client;
		private LidgrenMessageReceiver _receiver;

		public bool enableLogging = false;

		/// <summary>
		/// Automatically reconnect after a failed connection attempt when enabled.
		/// Default: FALSE.
		/// </summary>
		public bool PersistConnection = false;
		public bool AutoReconnect = false;

		public bool IsConnected { get; private set; }
		public bool IsConnecting { get; private set; }

		public delegate void ConnectionEvent();

		public delegate void DisconnectionEvent(byte disconnectMsg);

		public delegate void RedirectEvent();

		public event ConnectionEvent OnConnected = delegate { };
		public event ConnectionEvent OnWillDisconnect = delegate { };
		public event ConnectionEvent OnConnectFailed = delegate { };
		public event DisconnectionEvent OnDisconnected = delegate { };
		public event RedirectEvent OnWillRedirect = delegate { };
		public event RedirectEvent OnDidRedirect = delegate { };

		protected string hostname;
		protected int port;

		private byte lastDisconnectMsg = 0;

		protected virtual Action<NetOutgoingMessage> GetAuthenticator()
		{
			return null;
		}

		public int Latency
		{
			get
			{
				if (_client == null || _client.ServerConnection == null || _client.ServerConnection.Status != NetConnectionStatus.Connected)
					return 0;
				
				return (int) (_client.ServerConnection.AverageRoundtripTime * 1000);
			}
		}

		public NetOutgoingMessage CreateOutgoingMessage()
		{
			return _client.CreateMessage();
		}

		private void SetupClient()
		{
			if (_client == null)
			{
				_client = new NetClient(LidgrenUtils.CreateClientConfiguration("Default"));
				_client.Start();

				_receiver = new LidgrenMessageReceiver(_client, OnReceiveMessage);
			}
		}

		private void DestroyClient()
		{
			if (_client != null)
			{
				_client.Shutdown(string.Empty);
				_receiver.Dispose();

				_client = null;
				_receiver = null;
			}
		}

		#region Scope Registration

		private Dictionary<short, BaseClientScope> activeScopes = new Dictionary<short, BaseClientScope>();
		private Dictionary<byte, BaseClientScope> inactiveScopes = new Dictionary<byte, BaseClientScope>();

		public TClientScope RegisterScope<TClientScope>(byte scopeIdentifier) where TClientScope : BaseClientScope, new()
		{
			// create scope
			TClientScope scope = new TClientScope();

			scope.SetScopeIdentifier(scopeIdentifier);

			// add it to inactive scope using the identifier as key
			inactiveScopes[scopeIdentifier] = scope;

			return scope;
		}

		public void UnregisterScope(BaseClientScope scope)
		{
			if (activeScopes.Remove(scope.msgType))
			{
				// make sure to dispose of it as well
				scope.Dispose();
			}
			else
				NetworkDebug.LogWarningFormat("Failed to remove the Scope {0} because it was not registered with the MasterServer.", scope);
		}

		#endregion

		#region Connection Operations

		public void Connect(string serverHost, int serverPort)
		{
			if (IsConnected)
				throw new Exception("Client is already connected to a server");

			if (IsConnecting)
				throw new Exception("Client is already attempting to connect to a server");

			// set last hostname and port in order to be able to connect client
			this.hostname = serverHost;
			this.port = serverPort;

			// finally, connect the client
			ConnectClient();
		}

		public void ReconnectTo(string newHostname)
		{
			hostname = newHostname;

			Disconnect();
			Reconnect();
		}

		public void Reconnect()
		{
			ConnectClient();
		}

		private void ConnectClient()
		{
			if (_client == null)
				SetupClient();

			if (enableLogging)
				NetworkDebug.LogFormat("[MasterClient] Connecting to {0}:{1}", hostname, port);

			IsConnecting = true;

			try
			{
				NetOutgoingMessage hailMessage = MakeAuthenticationMessage();
				IPAddress address = NetUtility.Resolve(hostname);
				_client.Connect(new IPEndPoint(address, port), hailMessage);
			}
			catch (Exception e)
			{
				OnLidgrenDisconnected();
				NetworkDebug.LogException(e);
			}
		}

		public void Disconnect()
		{
			if (IsConnected || IsConnecting)
			{
				if (enableLogging)
					NetworkDebug.Log("[MasterClient] Manual disconnect");

				CleanActiveScopes();

				DestroyClient();

				IsConnected = false;
				IsConnecting = false;

				OnDisconnected(lastDisconnectMsg);

				if (AutoReconnect)
					Reconnect();
			}
			else
			{
				if (enableLogging)
					NetworkDebug.Log("[MasterClient] Manual disconnect ignored because client is not connected or establishing a connection");
			}
		}

		#endregion

		private void CleanActiveScopes()
		{
			// manually exit all scopes
			foreach (BaseClientScope scope in activeScopes.Values)
			{
				inactiveScopes.Add(scope.scopeIdentifier, scope);

				// notify the client that the Scope is no longer ready to send and receive Signals with its counterpart server-side Scope
				scope.ExitScope();

				scope.Dispose();
			}

			// clear active scopes
			activeScopes.Clear();
		}

		private void OnReceiveMessage(NetIncomingMessage msg)
		{
			switch (msg.MessageType)
			{
				case NetIncomingMessageType.StatusChanged:
				{
					NetConnectionStatus status = (NetConnectionStatus) msg.ReadByte();

					if (status == NetConnectionStatus.Connected)
					{
						OnLidgrenConnected();
					}
					else if (status == NetConnectionStatus.Disconnected)
					{
						OnLidgrenDisconnected();
					}

					break;
				}
				case NetIncomingMessageType.Data:
					ProcessSignal(msg);
					break;
//				default:
//					LidgrenUtilities.ParseMessage("[Client] ", msg);
//					break;
			}
		}

		private NetOutgoingMessage MakeAuthenticationMessage()
		{
			Action<NetOutgoingMessage> authSender = GetAuthenticator();

			if (authSender != null)
			{
				// create message and let authenticator write custom auth data to it
				NetOutgoingMessage msg = CreateOutgoingMessage();
				authSender(msg);
				return msg;
			}
			return null;
		}

		void ProcessSignal(NetIncomingMessage msg)
		{
            short msgType = msg.ReadInt16();

			BaseClientScope targetScope;
			if (activeScopes.TryGetValue(msgType, out targetScope))
			{
				targetScope.ProcessSignal(msg);
			}
			else
			{
				switch (msgType)
				{
					case ScopeMsgType.EnterScope:
						OnReceiveEnterScope(msg);
						break;
					case ScopeMsgType.ExitScope:
						OnReceiveExitScope(msg);
						break;
					case ScopeMsgType.SwitchScope:
						OnReceiveSwitchScope(msg);
						break;
					case ScopeMsgType.DisconnectMessage:
						OnReceiveDisconnectMessage(msg);
						break;
					case ScopeMsgType.RedirectMessage:
						OnReceiveRedirectMessage(msg);
						break;
					default:
					{
						NetworkDebug.Log("Unhandled message received.");
						break;
					}
				}
			}
		}

		#region Connection Events

		void OnLidgrenConnected()
		{
			if (enableLogging)
				NetworkDebug.Log("[MasterClient] Connected to Server");

			IsConnecting = false;
			IsConnected = true;

			lastDisconnectMsg = 0;

			// initialize client scope on connect
			OnConnected();
		}

		void OnLidgrenDisconnected()
		{
			if (IsConnected)
			{
				if (enableLogging)
					NetworkDebug.Log("[MasterClient] Disconnected from Server");
				IsConnected = false;

				OnWillDisconnect();

				CleanActiveScopes();

				DestroyClient();

				OnDisconnected(lastDisconnectMsg);

				if (AutoReconnect && lastDisconnectMsg != 0)	// 0 means invalid credentials
					Reconnect();
			}
			else if (IsConnecting)
			{
				DestroyClient();

				if (enableLogging)
					NetworkDebug.Log("[MasterClient] Could not establish a connection within the timeout period");

				OnConnectFailed();

				// retry to connect
				if (PersistConnection)
					ConnectClient();
			}
			
			IsConnecting = false;
		}

		void OnReceiveEnterScope(NetIncomingMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.ReadInt16();

			// 2. read scopeIdentifier
			byte scopeIdentifier = msg.ReadByte();

			ProcessEnterScope(scopeIdentifier, scopeMsgType, msg);
		}

		void OnReceiveExitScope(NetIncomingMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.ReadInt16();

			ProcessExitScope(scopeMsgType);
		}

		void OnReceiveSwitchScope(NetIncomingMessage msg)
		{
			// 1. read msgType of prevScope
			short prevScopeMsgType = msg.ReadInt16();

			// 2. read msgType of newScope
			short newScopeMsgType = msg.ReadInt16();

			// 3. read scopeIdentifier of newScope
			byte newScopeIdentifier = msg.ReadByte();

			ProcessExitScope(prevScopeMsgType);
			ProcessEnterScope(newScopeIdentifier, newScopeMsgType, msg);
		}

		void OnReceiveDisconnectMessage(NetIncomingMessage msg)
		{
			lastDisconnectMsg = msg.ReadByte();

			OnLidgrenDisconnected();
		}

		void OnReceiveRedirectMessage(NetIncomingMessage msg)
		{
			string hostname = msg.ReadString();
			int port = msg.ReadInt32();

			if (hostname == "127.0.0.1")
				this.hostname = hostname;

			if (enableLogging)
				NetworkDebug.LogFormat("Redirected to {0}:{1}", hostname, port);

			if (IsRedirecting)
				throw new Exception("Already being redirected. Ignoring redirect message.");

			ProcessRedirect(hostname, port);
		}

		public bool IsRedirecting { get; private set; }

		protected void ProcessRedirect(string hostname, int port)
		{
			IsRedirecting = true;

			OnWillRedirect();

			// set the new address and port and connect
			this.hostname = hostname;
			this.port = port;

			// just disconnect - the child class will take care of reconnecting to the previously set endpoint
			Disconnect();

			OnConnected += MasterClient_OnRedirectComplete;
		}

		void MasterClient_OnRedirectComplete()
		{
			OnConnected -= MasterClient_OnRedirectComplete;

			IsRedirecting = false;

			OnDidRedirect();
		}

		#endregion

		private void ProcessEnterScope(byte scopeIdentifier, short msgType, NetIncomingMessage extraData)
		{
			BaseClientScope scope;

			// find it in the inactive scopes
			if (!inactiveScopes.TryGetValue(scopeIdentifier, out scope))
			{
				NetworkDebug.LogWarning("Active Scopes");
				foreach (var sc in activeScopes.Values)
					NetworkDebug.LogWarning(sc.GetType());
				NetworkDebug.LogWarning("Inactive Scopes");
				foreach (var sc in inactiveScopes.Values)
					NetworkDebug.LogWarning(sc.GetType());

				throw new Exception(string.Format("Received an EnterScope message for a scope that is not active (ID={0}).", scopeIdentifier));
			}

			// remove from inactive and add to active scopes
			inactiveScopes.Remove(scopeIdentifier);
			activeScopes.Add(msgType, scope);

			// initialize the scope with the specified connection
			scope.Initialize(msgType, _client, this);

			// notify the client that the Scope is ready to send and receive Signals with its counterpart server-side Scope
			scope.EnterScope(extraData);
		}

		private void ProcessExitScope(short msgType)
		{
			BaseClientScope scope;

			if (!activeScopes.TryGetValue(msgType, out scope))
				throw new Exception("Received an ExitScope message for a scope that is not registered.");

			// remove from active and add to inactive scopes
			activeScopes.Remove(msgType);
			inactiveScopes.Add(scope.scopeIdentifier, scope);

			// notify the client that the Scope is no longer ready to send and receive Signals with its counterpart server-side Scope
			scope.ExitScope();

			scope.Dispose();
		}
	}
}