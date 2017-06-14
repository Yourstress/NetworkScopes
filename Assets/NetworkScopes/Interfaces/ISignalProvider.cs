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
}