


namespace NetworkScopes
{
	using System.Collections.Generic;
	using System;
	using Lidgren.Network;

	public interface IPeerMessageSender<TPeer> : IMessageSender, IPeerTarget<TPeer> where TPeer : NetworkPeer
	{
	}

	public interface IMessageSender
	{
		OutgoingNetworkPacket CreatePacket(int signalType);
		void SendPacket(NetworkPacket packet);
	}

	public abstract class ClientScope : BaseClientScope
	{
		#region Signal Queuing
		private bool _signalQueueEnabled = false;
		private Queue<NetworkPacket> queuedSignalWriters = null;

		public bool SignalQueueEnabled
		{
			get { return _signalQueueEnabled; }
			set
			{
				_signalQueueEnabled = value;

				if (value)
					queuedSignalWriters = new Queue<NetworkPacket>(3);
				else
					queuedSignalWriters = null;

			}
		}
		#endregion

		protected override void OnEnterScope()
		{
			IsPaused = false;

			// right after entering the scope, send out all queued signals
			while (_signalQueueEnabled && queuedSignalWriters.Count > 0)
			{
				NetworkPacket msg = queuedSignalWriters.Dequeue();

				// TODO:
//				// modify the 2nd and 3rd bytes in the array containing the possibly incorrect msgType
//				byte[] writerBytes = msg.Data;
//				byte[] msgTypeBytes = BitConverter.GetBytes(msgType);
//
//				// replace the bytes within the internal array
//				Buffer.BlockCopy(msgTypeBytes, 0, writerBytes, 2, sizeof(short));
//
//				IMessageSender sender = this;
//				sender.PrepareAndSendWriter(msg);
			}

			base.OnEnterScope();
		}

		public override void SendPacket(NetworkPacket packet)
		{
			// only send the writer if the Scope is active
			if (IsActive)
			{
				//				writer.FinishMessage();

#if UNITY_EDITOR && SCOPE_DEBUGGING
// log outgoing signal
				ScopeDebugger.AddOutgoingSignal (this, typeof(TServerScope), new NetworkReader (writer.ToArray ()));
#endif
				NetOutgoingMessage msg = packet.CreateOutgoingMessage(MasterClient.client);

				client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
			}
			// otherwise, ignore or queue the Signal for later
			else
			{
				// if Signal queuing is enabled, enqueue the writer to be sent later
				if (_signalQueueEnabled)
					queuedSignalWriters.Enqueue(packet);
				// otherwise, just display a warning message
				else
					NetworkDebug.LogWarningFormat("Ignoring Signal sending because the Scope <color=white>{0}</color> is no longer active", GetType().Name);
			}
		}
	}

}
