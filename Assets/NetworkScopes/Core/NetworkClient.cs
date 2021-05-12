using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetworkScopes.ServiceProviders.LiteNetLib;

namespace NetworkScopes
{
	public enum NetworkState
	{
		Offline,
		Connecting,
		Connected,
	}
	public abstract class NetworkClient : IClientProvider, IClientSignalProvider, IDisposable
	{
		private readonly Dictionary<ScopeChannel,IClientScope> activeScopes = new Dictionary<ScopeChannel, IClientScope>();
		private readonly Dictionary<ScopeIdentifier,IClientScope> inactiveScopes = new Dictionary<ScopeIdentifier, IClientScope>();

		// IClientProvider
		public abstract bool IsConnecting { get; protected set; }
		public abstract bool IsConnected { get; protected set; }
		public bool IsRedirecting { get; private set; }
		
		public abstract int LatencyInMs { get; protected set; }

		public NetworkState State => IsConnected ? NetworkState.Connected : IsConnecting ? NetworkState.Connecting : NetworkState.Offline;
		
		public event Action OnConnected = delegate {};
		public event Action OnConnectFailed = delegate {};
		public event Action<byte> OnDisconnected = delegate {};
		public event Action<NetworkState> OnStateChanged = delegate {};
		public event Action OnWillRedirect = delegate {};
		public event Action OnDidRedirect = delegate {};

		public bool enableLogging = true;
		private readonly string _logCategory;
		
		protected abstract void ConnectInternal(string hostnameOrIP, int port);
		protected abstract void DisconnectInternal();

		// IClientSignalProvider
		public abstract ISignalWriter CreateSignal(short channelId);
		public abstract void SendSignal(ISignalWriter signal);
		
		// Other
		public bool AutoRetryFailedConnection = false;
		public bool AutoReconnect = false;
		public TimeSpan AutoReconnectDelay = TimeSpan.FromSeconds(5);
		
		private string lastHost;
		private int lastPort;
		
		private byte lastDisconnectMsg;
		
		public abstract void Dispose();

		public NetworkClient()
		{
			_logCategory = GetType().Name;
		}

		#region Setup
		public TClientScope RegisterScope<TClientScope>(ScopeIdentifier scopeIdentifier) where TClientScope : IClientScope, new()
		{
			return RegisterScope<TClientScope>(new TClientScope(), scopeIdentifier);
		}
		
		public TClientScope RegisterScope<TClientScope>(TClientScope scope, ScopeIdentifier scopeIdentifier) where TClientScope : IClientScope
		{
			scope.Initialize(this, scopeIdentifier);

			inactiveScopes[scopeIdentifier] = scope;
			return scope;
		}

		public void UnregisterScope(IClientScope scope)
		{
			if (activeScopes.Remove(scope.channel))
			{
				// clean up the scope
				scope.Dispose();
			}
			else
			{
				NSDebug.Log($"Failed to remove the Scope {scope} because it was not registered with the client.");
			}
		}
		#endregion
		
		#region Connect/Disconnect
		public void Connect(string hostOrIP, int port)
		{
			lastHost = hostOrIP;
			lastPort = port;
			
			if (enableLogging)
				NSDebug.Log(_logCategory, $"Client connecting to {hostOrIP}:{port}");
			
			// connect
			ConnectInternal(hostOrIP, port);
			
			// update status
			OnStateChanged(NetworkState.Connecting);
		}

		public void Disconnect()
		{
			if (enableLogging)
				NSDebug.Log(_logCategory, $"Disconnecting from {lastHost}:{lastPort}");
			
			WillDisconnect();
			
			DisconnectInternal();
		}
		
		public async void Reconnect(TimeSpan? delay)
		{
			if (string.IsNullOrEmpty(lastHost))
				throw new Exception("Connect must be called first.");

			if (delay.HasValue)
				await Task.Delay(delay.Value);
			
			Connect(lastHost, lastPort);
		}
		#endregion

		#region Internal Network Callbacks
		protected void DidConnect()
		{
			IsConnected = true;
			lastDisconnectMsg = 0;

			// if this was a redirect, unset the redirecting flag and raise event
			if (IsRedirecting)
			{
				IsRedirecting = false;
				OnDidRedirect();
			}
			
			if (enableLogging)
				NSDebug.Log(_logCategory, $"Connected to {lastHost}:{lastPort}");
			
			// raise events
			OnConnected();
			OnStateChanged(State);
		}

		protected void WillDisconnect()
		{
			foreach (IClientScope activeScope in activeScopes.Values)
			{
				// exit the scope
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
				NSDebug.Log(_logCategory, $"[{GetType().Namespace}] Failed to connect to {lastHost}:{lastPort}");
			
			// raise events
			OnConnectFailed();
			OnStateChanged(State);
			
			OnConnectFailed();
			
			if (AutoRetryFailedConnection)
				Reconnect(AutoReconnectDelay);
		}

		protected void DidDisconnect()
		{
			IsConnecting = false;
			IsConnected = false;
			
			if (enableLogging)
				NSDebug.Log(_logCategory, $"Disconnected from {lastHost}:{lastPort}");
			
			// raise events
			OnDisconnected(lastDisconnectMsg);
			OnStateChanged(State);
			
			if (AutoReconnect)
				Reconnect(AutoReconnectDelay);
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
				NSDebug.LogWarning($"Client could not process signal on unknown channel {targetChannel}.");
			}
		}

		protected void ProcessSystemSignal(ScopeChannel systemChannel, ISignalReader signal)
		{
			switch (systemChannel)
			{
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

				case ScopeChannel.RedirectMessage:
				{
					// read hostName and port to redirect to
					string hostName = signal.ReadString();
					int port = signal.ReadInt32();

					ProcessRedirectMessage(hostName, port);
					
					break;
				}
				
				case ScopeChannel.DisconnectMessage:
					
					// read disconnect message
					lastDisconnectMsg = signal.ReadByte();
					
					// and disconnect immediately
					Disconnect();

					break;
					
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
					NSDebug.LogWarning($"Failed to enter scope. No client scope is registered with the identifier {scopeID}.");
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
					NSDebug.LogWarning($"Failed to exit scope. No client scope is registered on channel {channel}.");
				}
			}

			void ProcessSwitchScope(ScopeChannel prevChannel, ScopeChannel newChannel, ScopeIdentifier scopeId)
			{
				ProcessExitScope(prevChannel);
				ProcessEnterScope(newChannel, scopeId);
			}
			
			void ProcessRedirectMessage(string hostName, int port)
			{
				if (IsRedirecting)
					throw new Exception("Already being redirected. Ignoring redirect message.");
				
				if (enableLogging)
					NSDebug.Log(_logCategory, $"Redirected to {hostName}:{port}");

				IsRedirecting = true;

				OnWillRedirect();

				// disconnect - and make sure auto reconnect is disabled
				bool prevAutoReconnect = AutoReconnect;
				AutoReconnect = false;
				Disconnect();
				
				// set the new address and port and connect
				lastHost = hostName;
				lastPort = port;
				
				Reconnect(TimeSpan.Zero);
				AutoReconnect = prevAutoReconnect;
			}
		}
		#endregion

		public static NetworkClient CreateLiteNetLibClient()
		{
			return new LiteNetClient();
		}
	}
}