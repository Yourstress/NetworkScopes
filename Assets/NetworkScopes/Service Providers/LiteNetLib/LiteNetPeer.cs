using LiteNetLib;

namespace NetworkScopes.ServiceProviders.LiteNetLib
{
    public class LiteNetPeer : NetworkPeer
    {
        public readonly NetPeer _peer;

        public LiteNetPeer(NetPeer peer)
        {
            _peer = peer;
        }

        public override string ipAddress => _peer.EndPoint.Address.ToString();
        
        public override void Disconnect()
        {
            _peer.Disconnect();
        }

        protected override void Cleanup()
        {
        }

        public override void SendSignal(ISignalWriter signal)
        {
            _peer.Send(signal as SignalWriter, 0, DeliveryMethod.ReliableOrdered);
        }
    }
}