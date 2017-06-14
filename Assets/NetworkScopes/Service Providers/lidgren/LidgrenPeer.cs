using Lidgren.Network;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public class LidgrenPeer : NetworkPeer
	{
		public readonly NetConnection connection;

		public LidgrenPeer(NetConnection connection)
		{
			this.connection = connection;
		}

		public override void SendSignal(ISignalWriter signal)
		{
			connection.SendMessage(((LidgrenSignalWriter) signal).netMessage, NetDeliveryMethod.ReliableOrdered, 0);
		}
	}
}