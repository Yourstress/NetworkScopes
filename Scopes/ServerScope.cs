
using Lidgren.Network;

namespace NetworkScopes
{
	public abstract class ServerScope<TPeer> : BaseServerScope<TPeer>, IPeerMessageSender<TPeer> where TPeer : NetworkPeer, new()
	{
		public override NetOutgoingMessage CreateWriter(int signalType)
		{
			NetOutgoingMessage msg = Master.CreateOutgoingMessage();

			msg.Write(msgType);
			msg.Write(signalType);

			return msg;
		}

		public override void PrepareAndSendWriter(NetOutgoingMessage msg)
		{
			if (!IsTargetGroup)
				ScopeUtils.SendRawMessage(msg, TargetPeer);
			else
				ScopeUtils.SendRawMessage(msg, TargetPeerGroup, Master.netServer);
		}

		public override void SendNetworkVariableWriter(NetOutgoingMessage msg)
		{
			TargetPeerGroup = Peers;
			PrepareAndSendWriter(msg);
		}
	}
}
