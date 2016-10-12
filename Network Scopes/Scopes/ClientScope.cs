

namespace NetworkScopesV2
{
	using System.Collections.Generic;

	public interface INetworkSender
	{
		IMessageWriter CreateWriter(int signalType);
		void PrepareAndSendWriter(IMessageWriter writer);
	}

	public abstract class ClientScope : BaseClientScope, INetworkSender
	{
		#region Signal Queuing
		private bool _signalQueueEnabled = false;
		private Queue<IMessageWriter> queuedSignalWriters = null;

		public bool SignalQueueEnabled
		{
			get { return _signalQueueEnabled; }
			set
			{
				_signalQueueEnabled = value;

				if (value)
					queuedSignalWriters = new Queue<IMessageWriter>(3);
				else
					queuedSignalWriters = null;
					
			}
		}
		#endregion

		protected override void OnEnterScope ()
		{
			IsPaused = false;

			// right after entering the scope, send out all queued signals
			while (_signalQueueEnabled && queuedSignalWriters.Count > 0)
			{
				IMessageWriter writer = queuedSignalWriters.Dequeue();

				// modify the 2nd and 3rd bytes in the array containing the possibly incorrect msgType
//				byte[] writerBytes = writer.AsArray();
//				byte[] msgTypeBytes = BitConverter.GetBytes(msgType);
//
//				// replace the bytes within the internal array
//				Buffer.BlockCopy(msgTypeBytes, 0, writerBytes, 2, sizeof(short));

				((INetworkSender)this).PrepareAndSendWriter(writer);
			}

			base.OnEnterScope ();
		}

		IMessageWriter INetworkSender.CreateWriter(int signalType)
		{
			return Client.CreateWriter(scopeChannel, signalType);
		}

		void INetworkSender.PrepareAndSendWriter(IMessageWriter writer)
		{
			// only send the writer if the Scope is active
			if (IsActive)
			{
				Client.PrepareAndSendWriter(writer);
			}
			// otherwise, ignore or queue the Signal for later
			else
			{
				// if Signal queuing is enabled, enqueue the writer to be sent later
				if (_signalQueueEnabled)
					queuedSignalWriters.Enqueue(writer);
				// otherwise, just display a warning message
				else
					ScopeUtils.Log("Ignoring Signal sending because the Scope <color=white>{0}</color> is no longer active", GetType().Name);
			}
		}
	}

}
