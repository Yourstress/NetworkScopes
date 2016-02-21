using System.Collections.Generic;


namespace NetworkScopes
{
	using System;
	using UnityEngine;
	using UnityEngine.Networking;
	using UnityEngine.Assertions;
	using NetworkScopes;

	public abstract class MasterClient
	{
		NetworkClient client;

		public bool enableLogging = false;

		public bool IsConnected { get; private set; }
		public bool IsConnecting { get; private set; }

		public delegate void ConnectionEvent();

		public event ConnectionEvent OnConnected = delegate {};
		public event ConnectionEvent OnDisconnected = delegate {};

		private string serverHost;
		private int serverPort;

		public MasterClient()
		{
			client = new NetworkClient();
			ConnectionConfig conConfig = new ConnectionConfig();
			conConfig.AddChannel(QosType.ReliableSequenced);

			HostTopology topology = new HostTopology(conConfig, 3000);

			client.Configure(topology);

			client.RegisterHandler(MsgType.Connect, OnConnect);
			client.RegisterHandler(MsgType.Disconnect, OnDisconnect);
			client.RegisterHandler(MsgType.Error, OnError);
			client.RegisterHandler(ScopeMsgType.EnterScope, OnEnterScope);
			client.RegisterHandler(ScopeMsgType.ExitScope, OnExitScope);
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

				// unregister it forcefully
				client.UnregisterHandler(scope.msgType);
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

		private void ConnectClient()
		{
			if (enableLogging)
				Debug.LogFormat("[MasterClient] Connecting to {0}:{1}", serverHost, serverPort);
			
			client.Connect(serverHost, serverPort);
		}

		public void Disconnect()
		{
			if (IsConnected || IsConnecting)
			{
				Debug.Log("[MasterClient] Manual disconnect");

				client.Disconnect();

				IsConnected = false;
				IsConnecting = false;

				OnDisconnected();
			}
			else
			{
				Debug.Log("[MasterClient] Manual disconnect ignored because client is not connected or establishing a connection");
			}
		}
		#endregion

		void SetConnected(NetworkConnection connection)
		{
			IsConnecting = false;
			IsConnected = true;

			// initialize client scope on connect
			OnConnected();
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

				OnDisconnected();
			}
			else
			{
				Debug.Log("[MasterClient] Could not establish a connection within the timeout period");

				// retry to connect
				ConnectClient();
			}

			client.UnregisterHandler(100);
		}

		void OnError(NetworkMessage msg)
		{
			Debug.LogError("[MasterClient] Encountered a connection error");

			IsConnected = false;
			IsConnecting = false;
		}

		void OnEnterScope(NetworkMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.reader.ReadInt16();

			// 2. read scopeIdentifier
			byte scopeIdentifier = msg.reader.ReadByte();

			BaseClientScope scope;

			// find it in the inactive scopes
			if (!inactiveScopes.TryGetValue(scopeIdentifier, out scope))
				throw new Exception("Received an EnterScope message for a scope that is not registered.");

			// remove from inactive and add to active scopes
			inactiveScopes.Remove(scopeIdentifier);
			activeScopes.Add(scopeMsgType, scope);

			// initialize the scope with the specified connection
			scope.Initialize(scopeMsgType, client, this);

			// notify the client that the Scope is ready to send and receive Signals with its counterpart server-side Scope
			scope.EnterScope();
		}

		void OnExitScope(NetworkMessage msg)
		{
			// 1. read msgType
			short scopeMsgType = msg.reader.ReadInt16();

			BaseClientScope scope;

			if (!activeScopes.TryGetValue(scopeMsgType, out scope))
				throw new Exception("Received an ExitScope message for a scope that is not registered.");

			// remove from active and add to inactive scopes
			activeScopes.Remove(scopeMsgType);
			inactiveScopes.Add(scope.scopeIdentifier, scope);

			// notify the client that the Scope is no longer ready to send and receive Signals with its counterpart server-side Scope
			scope.ExitScope();

			scope.Dispose();
		}
		#endregion
	}
}
