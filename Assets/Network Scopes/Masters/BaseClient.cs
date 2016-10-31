
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;

	public abstract class BaseClient
	{
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

		protected byte lastDisconnectMsg = 0;

		protected abstract void ConnectInternal(string hostname, int port);
		protected abstract void DisconnectInternal();

		protected abstract void SetupClient();
		protected abstract void ShutdownClient();
		protected abstract void DestroyClient();
		protected abstract void SendInternal(IMessageWriter writer);

		#region INetworkSender implementation
		public abstract IMessageWriter CreateWriter (short msgType, int signalType);
		public abstract void PrepareAndSendWriter (IMessageWriter writer);
		#endregion

		#region Scope Handler Registration
		public delegate void ScopeHandlerDelegate(IMessageReader reader);
		private Dictionary<short,ScopeHandlerDelegate> _handlers = new Dictionary<short, ScopeHandlerDelegate> ();

		public void RegisterScopeHandler (short msgType, ScopeHandlerDelegate handler)
		{
			_handlers [msgType] = handler;
		}

		public void UnregisterScopeHandler (short msgType)
		{
			_handlers.Remove (msgType);
		}
		#endregion

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
			if (activeScopes.Remove(scope.scopeChannel))
			{
				// make sure to dispose of it as well
				scope.Dispose();
			}
			else
				ScopeUtils.LogError("Failed to remove the Scope {0} because it was not registered with the MasterServer.", scope);
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
			if (enableLogging)
				ScopeUtils.Log("[Client - {0}] Connecting to {1}:{2}", GetType().Name, serverHost, serverPort);

			IsConnecting = true;

			ConnectInternal(serverHost, serverPort);
		}

		public void Disconnect()
		{
			if (IsConnected || IsConnecting)
			{
				if (enableLogging)
					ScopeUtils.Log("[Client - {0}] Manual disconnect", GetType().Name);

				CleanActiveScopes();

				DestroyClient();

				IsConnected = false;
				IsConnecting = false;

				OnDisconnected(lastDisconnectMsg);
			}
			else
			{
				if (enableLogging)
					ScopeUtils.Log("[Client - {0}] Manual disconnect ignored because client is not connected or establishing a connection", GetType().Name);
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

		#region Connection Events
		protected void OnConnect()
		{
			if (enableLogging)
				ScopeUtils.Log("[Client - {0}] Connected to Server", GetType().Name);

			IsConnecting = false;
			IsConnected = true;

			lastDisconnectMsg = 0;

			// initialize client scope on connect
			OnConnected();
		}

		protected void OnDisconnect()
		{
			IsConnecting = false;

			if (IsConnected)
			{
				ScopeUtils.Log("[Client - {0}] Disconnected from Server", GetType().Name);
				IsConnected = false;

				OnWillDisconnect();

				CleanActiveScopes();

				ShutdownClient();

				OnDisconnected(lastDisconnectMsg);
			}
			else
			{
				DisconnectInternal();
				ShutdownClient();

				ScopeUtils.Log("[Client - {0}] Could not establish a connection within the timeout period", GetType().Name);
				
				OnConnectFailed();

				// retry to connect
				if (PersistConnection)
					ConnectClient();
			}
		}

		protected void OnError(string error)
		{
			ScopeUtils.LogError("[CLient - {0}] Encountered a connection error: {1}", GetType().Name, error);

			IsConnected = false;
			IsConnecting = false;
		}

		protected void OnScopeSignal(IMessageReader reader)
		{
			short scopeChannel = reader.ReadInt16();

			try
			{
				_handlers[scopeChannel].Invoke(reader);
			}
			catch
			{
				ScopeUtils.Log("No scope registered on channel " + scopeChannel);
			}
		}

		void OnRedirectMessage(IMessageReader reader)
		{
			string hostname = reader.ReadString();
			int port = reader.ReadInt32();

			if (hostname == "127.0.0.1")
				hostname = serverHost;

			if (enableLogging)
				ScopeUtils.Log("Redirected to {0}:{1}", hostname, port);

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

		protected void ProcessEnterScope(byte scopeIdentifier, short msgType)
		{
			BaseClientScope scope;

			// find it in the inactive scopes
			if (!inactiveScopes.TryGetValue(scopeIdentifier, out scope))
				throw new Exception(string.Format("Received an EnterScope message for a scope that is not active (ID={0}).", scopeIdentifier));

			// remove from inactive and add to active scopes
			inactiveScopes.Remove(scopeIdentifier);
			activeScopes.Add(msgType, scope);

			// initialize the scope with the specified connection
			scope.Initialize(msgType, this);

			// notify the client that the Scope is ready to send and receive Signals with its counterpart server-side Scope
			scope.EnterScope();
		}

		protected void ProcessExitScope(short msgType)
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
