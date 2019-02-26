
namespace NetworkScopes
{
	public abstract class ServerScope<TPeer> : BaseServerScope<TPeer>, IPeerMessageSender<TPeer> where TPeer : NetworkPeer, new()
	{
		public override void SendPacket(NetworkPacket packet)
		{
			if (!IsTargetGroup)
				ScopeUtils.SendPacket(packet, TargetPeer);
			else
				ScopeUtils.SendPacket(packet, TargetPeerGroup, Master.netServer);
		}
	}
}
