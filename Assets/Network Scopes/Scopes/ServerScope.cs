
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Networking;

	public abstract class ServerScope<TPeer> : BaseServerScope<TPeer>, INetworkSender where TPeer : NetworkPeer
	{
		NetworkWriter INetworkSender.CreateWriter(int signalType)
		{
			NetworkWriter writer = new NetworkWriter();
			writer.StartMessage(msgType);
			writer.Write(signalType);
			return writer;
		}

		void INetworkSender.PrepareAndSendWriter(NetworkWriter writer)
		{
			writer.FinishMessage();

			#if UNITY_EDITOR && SCOPE_DEBUGGING
			// log outgoing signal
			ScopeDebugger.AddOutgoingSignal (this, typeof(TClientScope), new NetworkReader (writer));
			#endif

			if (!IsTargetGroup)
				ScopeUtils.SendNetworkWriter(writer, TargetPeer);
			else
				ScopeUtils.SendNetworkWriter(writer, TargetPeerGroup);
		}
	}
}
