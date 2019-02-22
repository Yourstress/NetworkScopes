namespace NetworkScopes
{
	public interface INetworkAuthenticator<TPeer, T> where TPeer : NetworkPeer
	{
		bool Authenticate(TPeer peer, T authentication);
	}
}
