
namespace NetworkScopes
{
	public interface IServerProvider
	{
		bool IsListening { get; }
		bool StartListening(int port);
		void StopListening();
	}
}