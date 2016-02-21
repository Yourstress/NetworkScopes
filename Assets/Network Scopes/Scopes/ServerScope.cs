
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.Networking;

	public abstract class ServerScope<TPeer, TClientScope> : BaseServerScope<TPeer> where TPeer : IScopePeer where TClientScope : BaseClientScope
	{
		// used to strong-type Network calls and inject network message sends at post-compile time
		[NonSerialized]
		public TClientScope Client;

		protected NetworkWriter CreateWriter(int signalType)
		{
			NetworkWriter writer = new NetworkWriter();
			writer.StartMessage(msgType);
			writer.Write(signalType);
			return writer;
		}

		protected void PrepareAndSendWriter(NetworkWriter writer)
		{
			writer.FinishMessage();

			if (!IsTargetGroup)
				TargetPeer.connection.SendWriter(writer, 0);
			else
			{
				foreach (TPeer peer in TargetPeerGroup)
				{
					peer.connection.SendWriter(writer, 0);
				}
			}
		}
	}

}
