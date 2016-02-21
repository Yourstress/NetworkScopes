using System;


namespace NetworkScopes
{
	using UnityEngine.Networking;
	using System.Collections.Generic;

	public abstract class ClientScope<TServerPeer, TServerScope> : BaseClientScope where TServerScope : BaseServerScope<TServerPeer> where TServerPeer : IScopePeer
	{
		public TServerScope Server;

		#region Signal Queuing
		private bool _signalQueueEnabled = false;
		private Queue<NetworkWriter> queuedSignalWriters = null;

		public bool SignalQueueEnabled
		{
			get { return _signalQueueEnabled; }
			set
			{
				_signalQueueEnabled = value;

				if (value)
					queuedSignalWriters = new Queue<NetworkWriter>(3);
				else
					queuedSignalWriters = null;
					
			}
		}
		#endregion

		protected override void OnEnterScope ()
		{
			// right after entering the scope, send out all queued signals
			while (_signalQueueEnabled && queuedSignalWriters.Count > 0)
			{
				NetworkWriter writer = queuedSignalWriters.Dequeue();

				// modify the 2nd and 3rd bytes in the array containing the possibly incorrect msgType
				byte[] writerBytes = writer.AsArray();
				byte[] msgTypeBytes = BitConverter.GetBytes(msgType);

				// replace the bytes within the internal array
				Buffer.BlockCopy(msgTypeBytes, 0, writerBytes, 2, sizeof(short));

				PrepareAndSendWriter(writer);
			}

			base.OnEnterScope ();
		}

		protected NetworkWriter CreateWriter(int signalType)
		{
			NetworkWriter writer = new NetworkWriter();

			writer.StartMessage(msgType);
			writer.Write(signalType);
			return writer;
		}

		protected void PrepareAndSendWriter(NetworkWriter writer)
		{
			// only send the writer if the Scope is active
			if (IsActive)
			{
				writer.FinishMessage();
				client.connection.SendWriter(writer, 0);
			}
			// otherwise, ignore or queue the Signal for later
			else
			{
				// if Signal queuing is enabled, enqueue the writer to be sent later
				if (_signalQueueEnabled)
					queuedSignalWriters.Enqueue(writer);
				// otherwise, just display a warning message
				else
					UnityEngine.Debug.LogWarning("Ignoring Signal sending because the Scope <color=white>{0}</color> is no longer active");
			}
		}
	}

}
