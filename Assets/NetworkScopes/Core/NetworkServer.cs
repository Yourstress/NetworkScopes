
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;

	public class NetworkServer : IServerCallbacks
	{
		private List<IServerProvider> serverProviders;
		private List<BaseServerScope> scopes;

		public BaseServerScope defaultScope;
		private Dictionary<IServerAuthenticator,BaseServerScope> authenticatorTargets = null;

		public event Action<PeerEntity> OnPeerEntityConnected = delegate {};
		public event Action<PeerEntity> OnPeerEntityDisconnected = delegate {};

		public NetworkServer(int serverCapacity = 1)
		{
			scopes = new List<BaseServerScope>();
			serverProviders = new List<IServerProvider>(serverCapacity);
		}

		public TServerProvider AddServerProvider<TServerProvider>() where TServerProvider : IServerProvider, new()
		{
			TServerProvider provider = new TServerProvider();
			provider.Initialize(this);
			serverProviders.Add(provider);
			return provider;
		}

		public TServerScope CreateScope<TServerScope>(byte scopeIdentifier, bool setAsDefault) where TServerScope : BaseServerScope, new()
		{
			TServerScope scope = new TServerScope();

			scope.Initialize();

			// if specified, set as default scope for newly connected players
			if (setAsDefault || defaultScope == null)
				defaultScope = scope;

			scopes.Add(scope);

			return scope;
		}

		public TAuthenticator UseAuthenticator<TAuthenticator>(BaseServerScope targetScope) where TAuthenticator : IServerAuthenticator, new()
		{
			TAuthenticator authenticator = new TAuthenticator();

			// assign the default scope to which we'll hand-over the peers
			authenticator.targetScope = targetScope;

			if (!scopes.Contains(targetScope))
				throw new System.Exception("Could not register authenticator because the target scope was not initialized.");

			// register the authenticator as an entry point for new peers
			if (authenticatorTargets == null)
				authenticatorTargets = new Dictionary<IServerAuthenticator, BaseServerScope>(4);
			
			authenticatorTargets[authenticator] = targetScope;

			return authenticator;
		}

		#region IServerCallbacks implementation
		public void OnConnected (PeerEntity entity)
		{
			OnPeerEntityConnected(entity);
		}

		public void OnDisconnected (PeerEntity entity)
		{
			OnPeerEntityDisconnected(entity);
		}
		#endregion
	}
}