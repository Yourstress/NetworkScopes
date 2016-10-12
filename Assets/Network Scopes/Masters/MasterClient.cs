using UnityEngine.Networking.NetworkSystem;

namespace NetworkScopes
{
	using System;
	using UnityEngine;
	using UnityEngine.Networking;
	using UnityEngine.Assertions;
	using NetworkScopes;
	using System.Collections.Generic;

	public abstract class MasterClient
	{
		NetworkClient client;

		public bool enableLogging = false;

		/// <summary>
		/// Automatically reconnect after a failed connection attempt when enabled.
		/// Default: FALSE.
		/// </summary>
		public bool PersistConnection = false;

		public bool IsConnected { get; private set; }
		public bool IsConnecting { get; private set; }

		public delegate void ConnectionEvent();
		public delegate void DisconnectionEvent(byte disconnectMsg);
		public delegate void RedirectEvent();

		public event ConnectionEvent OnConnected = delegate {};
		public event ConnectionEvent OnWillDisconnect = delegate {};
		public event ConnectionEvent OnConnectFailed = delegate {};
		public event DisconnectionEvent OnDisconnected = delegate {};
		public event RedirectEvent OnWillRedirect = delegate {};
		public event RedirectEvent OnDidRedirect = delegate {};

		protected string serverHost;
		protected int serverPort;

		private byte lastDisconnectMsg = 0;

		public static ConnectionConfig CreateConnectionConfig()
		{
			ConnectionConfig conConfig = new ConnectionConfig();

			conConfig.NetworkDropThreshold = 20;
			conConfig.DisconnectTimeout = 5000;

			conConfig.AddChannel(QosType.ReliableSequenced);

			return conConfig;
		}

		private void SetupClient()
		{
			client = new NetworkClient();

			HostTopology topology = new HostTopology(CreateConnectionConfig(), 3000);

			client.Configure(topology);

			client.RegisterHandler(MsgType.Connect, OnConnect);
			client.RegisterHandler(MsgType.Disconnect, OnDisconnect);
			client.RegisterHandler(MsgType.Error, OnError);
			client.RegisterHandler(ScopeMsgType.EnterScope, OnEnterScope);
			client.RegisterHandler(ScopeMsgType.ExitScope, OnExitScope);
			client.RegisterHandler(ScopeMsgType.SwitchScope, OnSwitchScope);
			client.RegisterHandler(ScopeMsgType.DisconnectMessage, OnDisconnectMessage);
			client.RegisterHandler(ScopeMsgType.RedirectMessage, OnRedirectMessage);
		}

		private void DestroyClient()
		{
			client.UnregisterHandler(MsgType.Connect);
			client.UnregisterHandler(MsgType.Disconnect);
			client.UnregisterHandler(MsgType.Error);
			client.UnregisterHandler(ScopeMsgType.EnterScope);
			client.UnregisterHandler(ScopeMsgType.ExitScope);
			client.UnregisterHandler(ScopeMsgType.SwitchScope);
			client.UnregisterHandler(ScopeMsgType.DisconnectMessage);
			client.UnregisterHandler(ScopeMsgType.RedirectMessage);

			client.Disconnect();
			client.Shutdown();

			client = null;
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
				Debug.LogWarningFormat("Failed to remove the Scope {0} because it was not registered with the MasterServer.", scope);
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
			this.serverHost = serverHost;
			this.serverPort = serverPort;

			// finally, connect the client
			ConnectClient();
		}

		public void Reconnect()
		{
			ConnectClient();
		}

		private void ConnectClient()
		{
			if (client == null)
				SetupClient();
			
			if (enableLogging)
				Debug.LogFormat("[MasterClient] Connecting to {0}:{1}", serverHost, serverPort);

			IsConnecting = true;

			client.Connect(serverHost, serverPort);
		}

		public void Disconnect()
		{
			if (IsConnected || IsConnecting)
			{
				if (enableLogging)
					Debug.Log("[MasterClient] Manual disconnect");

				CleanActiveScopes();

				DestroyClient();

				IsConnected = false;
				IsConnecting = false;

				OnDisconnected(lastDisconnectMsg);
			}
			else
			{
				if (enableLogging)
					Debug.Log("[MasterClient] Manual disconnect ignored because client is not connected or establishing a connection");
			}
		}
		#endregion

		void SetConnected(NetworkConnection connection)
		{
			IsConnecting = false;
			IsConnected = true;

			lastDisconnectMsg = 0;

			// initialize client scope on connect
			OnConnected();
		}

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

		#region Connection Events
		void OnConnect(NetworkMessage msg)
		{
			if (enableLogging)
				Debug.Log("[MasterClient] Connected to Server");
				
			SetConnected(msg.conn);
		}

		void OnDisconnect(NetworkMessage msg)
		{
			IsConnecting = false;

			if (IsConnected)
			{
				Debug.Log("[MasterClient] Disconnected from Server");
				IsConnected = false;

				OnWillDisconnect();

				CleanActiveScopes();

				client.Shutdown();
				client = null;

				OnDisconnected(lastDisconnectMsg);
			}
			else
			{
				client.Disconnect();
				client.Shutdown();
				client = null;

//				SetupClient();

				Debug.Log("[MasterClient] Could not establish a connection within the timeout period");
				
				OnConnectFailed();

				// retry to connect
				if (PersistConnection)
					ConnectClient();
			}
		}

		void OnError(NetworkMessage msg)
		{
			Debug.LogError("[MasterClient] Encountered a connection error " + msg.ReadMessage<ErrorMessage>().ToString());

			IsConnected = false;
			IsConnecting = false;
		}

		void OnEnterScope(NetworkMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.reader.ReadInt16();

			// 2. read scopeIdentifier
			byte scopeIdentifier = msg.reader.ReadByte();

			ProcessEnterScope(scopeIdentifier, scopeMsgType);
		}

		void OnExitScope(NetworkMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.reader.ReadInt16();

			ProcessExitScope(scopeMsgType);
		}

		void OnSwitchScope(NetworkMessage msg)
		{
			// 1. read msgType of prevScope
			short prevScopeMsgType = msg.reader.ReadInt16();

			// 2. read msgType of newScope
			short newScopeMsgType = msg.reader.ReadInt16();

			// 3. read scopeIdentifier of newScope
			byte newScopeIdentifier = msg.reader.ReadByte();

			ProcessExitScope(prevScopeMsgType);
			ProcessEnterScope(newScopeIdentifier, newScopeMsgType);
		}

		void OnDisconnectMessage(NetworkMessage msg)
		{
			lastDisconnectMsg = msg.reader.ReadByte();

			Disconnect();
		}

		void OnRedirectMessage(NetworkMessage msg)
		{
			string hostname = msg.reader.ReadString();
			int port = msg.reader.ReadInt32();

			if (hostname == "127.0.0.1")
				hostname = serverHost;

			if (enableLogging)
				Debug.LogFormat("Redirected to {0}:{1}", hostname, port);

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
			serverHost = hostname;
			serverPort = port;

			// just disconnect - the child class will take care of reconnecting to the previously set endpoint
			Disconnect();

			OnConnected += MasterClient_OnRedirectComplete;
		}

		void MasterClient_OnRedirectComplete ()
		{
			OnConnected -= MasterClient_OnRedirectComplete;

			IsRedirecting = false;
			
			OnDidRedirect();
		}
		#endregion

		private void ProcessEnterScope(byte scopeIdentifier, short msgType)
		{
			BaseClientScope scope;

			// find it in the inactive scopes
			if (!inactiveScopes.TryGetValue(scopeIdentifier, out scope))
			{
				Debug.LogWarning("Active Scopes");
				foreach (var sc in activeScopes.Values)
					Debug.LogWarning(sc.GetType());
				Debug.LogWarning("Inactive Scopes");
				foreach (var sc in inactiveScopes.Values)
					Debug.LogWarning(sc.GetType());

				throw new Exception(string.Format("Received an EnterScope message for a scope that is not active (ID={0}).", scopeIdentifier));

			}

			// remove from inactive and add to active scopes
			inactiveScopes.Remove(scopeIdentifier);
			activeScopes.Add(msgType, scope);

			// initialize the scope with the specified connection
			scope.Initialize(msgType, client, this);

			// notify the client that the Scope is ready to send and receive Signals with its counterpart server-side Scope
			scope.EnterScope();
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
