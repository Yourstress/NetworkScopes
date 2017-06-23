namespace NetworkScopes
{
	public interface ISignalProvider
	{
		ISignalWriter CreateSignal(short scopeChannel);
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
		void UnregisterScope<TServerScope>(TServerScope scope) where TServerScope : IServerScope;
	}

	public interface IServerScopeProvider : IScopeRegistrar, IServerSignalProvider
	{

	}
}