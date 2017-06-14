using System;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public abstract class NetworkPeer : INetworkPeer
	{
		public abstract void SendSignal(ISignalWriter signal);

		public event Action<INetworkPeer> OnDisconnect;
	}
}