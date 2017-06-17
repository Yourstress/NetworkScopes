using System;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public abstract class NetworkPeer : INetworkPeer
	{
		public abstract string ipAddress { get; }

		public abstract void Disconnect();

		public void TriggerDisconnectEvent()
		{
			OnDisconnect(this);
		}

		public abstract void SendSignal(ISignalWriter signal);

		public event Action<INetworkPeer> OnDisconnect = delegate { };
	}
}