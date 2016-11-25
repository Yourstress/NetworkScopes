
namespace NetworkScopes
{
	using System.Collections.Generic;

	public class NetworkServer
	{
		private List<IServerProvider> serverProviders;
		private List<BaseServerScope> scopes;

		public BaseServerScope defaultScope;
		private Dictionary<IAuthenticator,BaseServerScope> authenticatorTargets = null;

		public NetworkServer(int serverCapacity = 1)
		{
			scopes = new List<BaseServerScope>();
			serverProviders = new List<IServerProvider>(serverCapacity);
		}

		public TServerProvider AddServerProvider<TServerProvider>() where TServerProvider : IServerProvider, new()
		{
			TServerProvider provider = new TServerProvider();
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

		public TAuthenticator UseAuthenticator<TAuthenticator>(BaseServerScope targetScope) where TAuthenticator : IAuthenticator, new()
		{
			TAuthenticator authenticator = new TAuthenticator();

			// assign the default scope to which we'll hand-over the peers
			authenticator.targetScope = targetScope;

			if (!scopes.Contains(targetScope))
				throw new System.Exception("Could not register authenticator because the target scope was not initialized.");

			// register the authenticator as an entry point for new peers
			if (authenticatorTargets == null)
				authenticatorTargets = new Dictionary<IAuthenticator, BaseServerScope>(4);
			
			authenticatorTargets[authenticator] = targetScope;

			return authenticator;
		}
	}
}