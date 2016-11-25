
namespace NetworkScopes
{
	public interface IServerCallbacks
	{
		void OnConnected(PeerEntity entity);
		void OnDisconnected(PeerEntity entity);
	}
	
}