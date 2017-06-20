using System;
using System.Collections.Generic;
using System.Linq;
using NetworkScopes.ServiceProviders.Lidgren;
using NetworkScopes.Utilities;
using UnityEngine;

namespace NetworkScopes
{
	public abstract class NetworkClient : IClientProvider, IClientSignalProvider
	{
		// IClientProvider
		public abstract bool IsConnecting { get; }
		public abstract bool IsConnected { get; }
		public abstract void Connect(string hostnameOrIP, int port);
		public abstract void Disconnect();

		public abstract event Action OnConnected;
		public abstract event Action OnDisconnected;

		private Dictionary<ScopeIdentifier,IClientScope> inactiveScopes = new Dictionary<ScopeIdentifier, IClientScope>();
		private Dictionary<ScopeChannel,IClientScope> activeScopes = new Dictionary<ScopeChannel, IClientScope>();

		// IClientSignalProvider
		public abstract ISignalWriter CreateSignal(short scopeChannel);
		public abstract void SendSignal(ISignalWriter signal);

		public TClientScope RegisterScope<TClientScope>(ScopeIdentifier scopeIdentifier) where TClientScope : IClientScope, new()
		{
			TClientScope scope = new TClientScope();
			scope.Initialize(this, scopeIdentifier);

			inactiveScopes[scopeIdentifier] = scope;
			return scope;
		}

		protected void ProcessSignal(ISignalReader signal)
		{
			ScopeChannel targetChannel = signal.ReadScopeChannel();
			IClientScope targetScope;

			if (targetChannel == ScopeChannel.SystemChannel)
			{
				ProcessSystemSignal(signal);
			}
			// in order to receive a signal, the receiving scope must be active (got an Entered Scope system message).
			else if (activeScopes.TryGetValue(targetChannel, out targetScope))
			{
				targetScope.ProcessSignal(signal);
			}
			else
			{
				Debug.LogWarningFormat("Client could not process signal on unknown channel {0}.", targetChannel);
			}
		}

		protected void ProcessSystemSignal(ISignalReader signal)
		{
			byte systemMessage = signal.ReadByte();

			switch (systemMessage)
			{
				case SystemMessage.EnterScope:
				{
					// scope identifier tells us which inactive scope has been activated (entered)
					ScopeIdentifier scopeID = signal.ReadScopeIdentifier();

					// this tells us which channel to bind this newly entered scope to
					ScopeChannel channel = signal.ReadScopeChannel();

					IClientScope targetScope;
					if (inactiveScopes.TryGetValue(scopeID, out targetScope))
					{
						// move the scope to the actives list
						inactiveScopes.Remove(scopeID);
						activeScopes.Add(channel, targetScope);

						targetScope.EnterScope(channel);
					}
					else
					{
						Debug.LogWarningFormat("Failed to enter scope. No client scope is registered with the identifier {0}.", scopeID);
					}
					break;
				}

				case SystemMessage.ExitScope:
				{
					// this tells us which channel the target scope will be on
					ScopeChannel channel = signal.ReadScopeChannel();

					IClientScope targetScope;
					if (activeScopes.TryGetValue(channel, out targetScope))
					{
						// move the scope back to the inactives list
						activeScopes.Remove(channel);
						inactiveScopes.Add(targetScope.scopeIdentifier, targetScope);

						targetScope.ExitScope();
					}
					else
					{
						Debug.LogWarningFormat("Failed to exit scope. No client scope is registered on channel {0}.", channel);
					}
					break;
				}
			}
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

		// Static factory methods
		public static LidgrenClient CreateLidgrenClient()
		{
			return new LidgrenClient();
		}
	}
}