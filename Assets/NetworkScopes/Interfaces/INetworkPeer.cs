using System;

namespace NetworkScopes
{
    public interface INetworkPeer
    {
        string ipAddress { get; }
        bool IsDestroyed { get; }

        void Disconnect();
        void SendSignal(ISignalWriter signal);
        
        void ForceDisconnect(bool closeConnection);
        void DisconnectWithMessage(byte disconnectMsgIdentifier);
        void SoftDisconnect();

        void Redirect(string hostname, int port);

        event Action<INetworkPeer> OnDisconnect;
    }
}