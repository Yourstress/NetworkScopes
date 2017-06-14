using System;

namespace NetworkScopes
{
	public interface INetworkPeer
	{
		void SendSignal(ISignalWriter signal);

		event Action<INetworkPeer> OnDisconnect;
	}
}