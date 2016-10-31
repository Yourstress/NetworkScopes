
namespace NetworkScopes
{
	public abstract class ServerScope<TPeer> : BaseServerScope<TPeer>, INetworkSender where TPeer : NetworkPeer
	{
		IMessageWriter INetworkSender.CreateWriter(int signalType)
		{
			IMessageWriter writer = Master.CreateWriter(ScopeMsgType.ScopeSignal);
			writer.Write(scopeChannel);
			writer.Write(signalType);
			return writer;
		}

		void INetworkSender.PrepareAndSendWriter(IMessageWriter writer)
		{
			#if UNITY_EDITOR && SCOPE_DEBUGGING
			// log outgoing signal
			ScopeDebugger.AddOutgoingSignal (this, typeof(TClientScope), new NetworkReader (writer));
			#endif

			if (!IsTargetGroup)
				Master.SendWriter(writer, TargetPeer);
			else
			{
				Master.SendWriter(writer, TargetPeerGroup);
			}
		}
	}
}
