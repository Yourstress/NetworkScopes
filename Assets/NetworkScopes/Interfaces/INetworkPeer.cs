using System;

namespace NetworkScopes
{
    public interface INetworkPeer
    {
        string ipAddress { get; }

        void Disconnect();
        void TriggerDisconnectEvent();
        void SendSignal(ISignalWriter signal);

        event Action<INetworkPeer> OnDisconnect;
    }
}