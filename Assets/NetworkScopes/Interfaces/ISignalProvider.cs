namespace NetworkScopes
{
	public interface INetworkServer : IServerProvider, IServerScopeProvider
	{
		public IServerScope defaultScope { get; set; }
	}
	public interface INetworkClient : IClientProvider, IScopeRegistrar
	{ }
	
	public interface ISignalProvider
	{
		ISignalWriter CreateSignal(short channelId);
	}

	public interface IClientSignalProvider : ISignalProvider
	{
		void SendSignal(ISignalWriter signal);
	}

	public interface IServerSignalProvider : ISignalProvider
	{
		void SendSignal(PeerTarget target, ISignalWriter writer);
	}

	public interface IScopeRegistrar
	{
		TServerScope RegisterScope<TServerScope>(byte scopeIdentifier) where TServerScope : IServerScope, new();
		void RegisterScope<TServerScope>(TServerScope newScope, byte scopeIdentifier) where TServerScope : IServerScope;
		void UnregisterScope<TServerScope>(TServerScope scope) where TServerScope : IServerScope;
	}

	public interface IServerScopeProvider : IScopeRegistrar, IServerSignalProvider
	{

	}
}