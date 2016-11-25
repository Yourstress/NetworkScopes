
namespace NetworkScopes
{
	public interface IServerProvider
	{
		int listenPort { get; }
		bool isListening { get; }

		void Initialize(IServerCallbacks serverCallbacks);
		void StartListening(int listenPort);
		void StopListening();
	}
}