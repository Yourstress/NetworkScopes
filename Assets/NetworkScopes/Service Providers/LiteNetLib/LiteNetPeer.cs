using System;
using LiteNetLib;

namespace NetworkScopes.ServiceProviders.LiteNetLib
{
    public class LiteNetPeer : NetworkPeer
    {
        public NetPeer _peer;
        
        public override string ipAddress => _peer.EndPoint.Address.ToString();
        
        public void InitializeNetPeer(NetPeer peer)
        {
            if (_peer != null)
                throw new Exception("NetPeer is already set.");
            
            _peer = peer;
        }

        public override void Disconnect()
        {
            _peer.Disconnect();
        }

        public override void SendSignal(ISignalWriter signal)
        {
            _peer.Send(signal as SignalWriter, DeliveryMethod.ReliableOrdered);
        }
    }
}