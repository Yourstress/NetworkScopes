using System;
using System.Collections.Generic;
using NetworkScopes.ServiceProviders.LiteNetLib;
using NetworkScopes.Utilities;

namespace NetworkScopes
{
	public enum NetworkState
	{
		Offline,
		Connecting,
		Connected,
	}
	public abstract class NetworkClient : IClientProvider, IClientSignalProvider
	{
		private readonly Dictionary<ScopeChannel,IClientScope> activeScopes = new Dictionary<ScopeChannel, IClientScope>();
		private readonly Dictionary<ScopeIdentifier,IClientScope> inactiveScopes = new Dictionary<ScopeIdentifier, IClientScope>();

		// IClientProvider
		public abstract bool IsConnecting { get; protected set; }
		public abstract bool IsConnected { get; protected set; }
		
		public abstract int LatencyInMs { get; protected set; }

		public NetworkState State => IsConnected ? NetworkState.Connected : IsConnecting ? NetworkState.Connecting : NetworkState.Offline;
		
		public event Action OnConnected = delegate {};
		public event Action OnConnectFailed = delegate {};
		public event Action<byte> OnDisconnected = delegate {};
		public event Action<NetworkState> OnStateChanged = delegate {};

		public bool enableLogging = true;
		
		protected abstract void ConnectInternal(string hostnameOrIP, int port);
		protected abstract void DisconnectInternal();

		// IClientSignalProvider
		public abstract ISignalWriter CreateSignal(short channelId);
		public abstract void SendSignal(ISignalWriter signal);
		
		// Other
		public bool AutoRetryFailedConnection = false;
		
		private string lastHost;
		private int lastPort;

		#region Setup
		public TClientScope RegisterScope<TClientScope>(ScopeIdentifier scopeIdentifier) where TClientScope : IClientScope, new()
		{
			TClientScope scope = new TClientScope();
			scope.Initialize(this, scopeIdentifier);

			inactiveScopes[scopeIdentifier] = scope;
			return scope;
		}
		#endregion
		
		#region Connect/Disconnect
		public void Connect(string hostOrIP, int port)
		{
			lastHost = hostOrIP;
			lastPort = port;
			
			if (enableLogging)
				Debug.Log($"Client connecting to {hostOrIP}:{port}");
			
			// connect
			ConnectInternal(hostOrIP, port);
			
			// update status
			OnStateChanged(NetworkState.Connecting);
		}

		public void Disconnect()
		{
			if (enableLogging)
				Debug.Log($"Disconnecting from {lastHost}:{lastPort}");
			
			WillDisconnect();
			
			DisconnectInternal();
		}
		
		public void Reconnect()
		{
			if (string.IsNullOrEmpty(lastHost))
				throw new Exception("Connect must be called first.");
			
			Connect(lastHost, lastPort);
		}
		#endregion

		#region Internal Network Callbacks
		protected void DidConnect()
		{
			IsConnected = true;
			
			// raise events
			OnConnected();
			OnStateChanged(State);
		}

		protected void WillDisconnect()
		{
			foreach (KeyValuePair<ScopeChannel,IClientScope> activeScopeKvp in activeScopes)
			{
				// exit the scope
				IClientScope activeScope = activeScopeKvp.Value;
				activeScope.ExitScope();

				// and add this scope to the inactives list
				inactiveScopes.Add(activeScope.scopeIdentifier, activeScope);
			}

			activeScopes.Clear();
		}

		protected void DidFailToConnect()
		{
			IsConnecting = false;
			
			if (enableLogging)
				Debug.Log($"[{GetType().Namespace}] Failed to connect to {lastHost}:{lastPort}");
			
			// raise events
			OnConnectFailed();
			OnStateChanged(State);
			
			OnConnectFailed();
		}

		protected void DidDisconnect(byte disconnectMsg)
		{
			IsConnecting = false;
			IsConnected = false;
			
			if (enableLogging)
				Debug.Log($"Disconnected from {lastHost}:{lastPort}");
			
			// raise events
			OnDisconnected(disconnectMsg);
			OnStateChanged(State);
			
			if (AutoRetryFailedConnection)
				Reconnect();
		}
		
		#endregion

		#region Network Packet Handling
		protected void ProcessSignal(ISignalReader signal)
		{
			ScopeChannel targetChannel = signal.ReadScopeChannel();

			if (targetChannel.IsSystemChannel)
			{
				ProcessSystemSignal(targetChannel, signal);
			}
			// in order to receive a signal, the receiving scope must be active (got an Entered Scope system message).
			else if (activeScopes.TryGetValue(targetChannel, out IClientScope targetScope))
			{
				targetScope.ProcessSignal(signal);
			}
			else
			{
				Debug.LogWarning($"Client could not process signal on unknown channel {targetChannel}.");
			}
		}

		protected void ProcessSystemSignal(ScopeChannel systemChannel, ISignalReader signal)
		{
			switch (systemChannel)
			{
				// TODO: switcheraoo
				case ScopeChannel.EnterScope:
				{
					// this tells us which channel to bind this newly entered scope to
					ScopeChannel channel = signal.ReadScopeChannel();
					
					// scope identifier tells us which inactive scope has been activated (entered)
					ScopeIdentifier scopeID = signal.ReadScopeIdentifier();

					ProcessEnterScope(channel, scopeID);
					break;
				}

				case ScopeChannel.ExitScope:
				{
					// this tells us which channel the target scope will be on
					ScopeChannel channel = signal.ReadScopeChannel();

					ProcessExitScope(channel);
					break;
				}
				
				case ScopeChannel.SwitchScope:
				{
					// read the prev and new channel in order to switch out existing scope
					ScopeChannel prevChannel = signal.ReadScopeChannel();
					ScopeChannel newChannel = signal.ReadScopeChannel();

					// read scope identifier to determine the type of scope to use
					ScopeIdentifier scopeId = signal.ReadScopeIdentifier();
					
					ProcessSwitchScope(prevChannel, newChannel, scopeId);
					
					break;
				}
			}

			void ProcessEnterScope(ScopeChannel channel, ScopeIdentifier scopeID)
			{
				if (inactiveScopes.TryGetValue(scopeID, out IClientScope targetScope))
				{
					// move the scope to the actives list
					inactiveScopes.Remove(scopeID);
					activeScopes.Add(channel, targetScope);

					targetScope.EnterScope(channel);
				}
				else
				{
					Debug.LogWarning($"Failed to enter scope. No client scope is registered with the identifier {scopeID}.");
				}
			}

			void ProcessExitScope(ScopeChannel channel)
			{
				if (activeScopes.TryGetValue(channel, out IClientScope targetScope))
				{
					// move the scope back to the inactives list
					activeScopes.Remove(channel);
					inactiveScopes.Add(targetScope.scopeIdentifier, targetScope);

					targetScope.ExitScope();
				}
				else
				{
					Debug.LogWarning($"Failed to exit scope. No client scope is registered on channel {channel}.");
				}
			}

			void ProcessSwitchScope(ScopeChannel prevChannel, ScopeChannel newChannel, ScopeIdentifier scopeId)
			{
				ProcessExitScope(prevChannel);
				ProcessEnterScope(newChannel, scopeId);
			}
		}
		#endregion
		
		public static NetworkClient CreateLiteNetLibClient()
		{
			return new LiteNetClient();
		}
	}
}