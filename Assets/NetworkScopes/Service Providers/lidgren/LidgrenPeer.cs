using Lidgren.Network;

namespace NetworkScopes.ServiceProviders.Lidgren
{
    public class LidgrenPeer : NetworkPeer
    {
        public readonly NetConnection connection;

        public override string ipAddress
        {
            get { return connection.RemoteEndPoint.Address.ToString(); }
        }

        public LidgrenPeer(NetConnection connection)
        {
            this.connection = connection;
        }

        public override void Disconnect()
        {
            connection.Disconnect(string.Empty);
        }

        public override void SendSignal(ISignalWriter signal)
        {
            connection.SendMessage(((LidgrenSignalWriter) signal).netMessage, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}