using System;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public abstract class NetworkPeer : INetworkPeer
	{
		public abstract string ipAddress { get; }

		public bool isDestroyed { get; private set; }

		public abstract void Disconnect();

		public void TriggerDisconnectEvent()
		{
			isDestroyed = true;

			OnDisconnect(this);
		}

		public abstract void SendSignal(ISignalWriter signal);

		public event Action<INetworkPeer> OnDisconnect = delegate { };
	}
}