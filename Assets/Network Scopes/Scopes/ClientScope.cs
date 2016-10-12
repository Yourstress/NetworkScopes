using System;


namespace NetworkScopes
{
	using UnityEngine.Networking;
	using System.Collections.Generic;

	public interface INetworkSender
	{
		NetworkWriter CreateWriter(int signalType);
		void PrepareAndSendWriter(NetworkWriter writer);
	}

	public abstract class ClientScope : BaseClientScope, INetworkSender
	{
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
			IsPaused = false;

			// right after entering the scope, send out all queued signals
			while (_signalQueueEnabled && queuedSignalWriters.Count > 0)
			{
				NetworkWriter writer = queuedSignalWriters.Dequeue();

				// modify the 2nd and 3rd bytes in the array containing the possibly incorrect msgType
				byte[] writerBytes = writer.AsArray();
				byte[] msgTypeBytes = BitConverter.GetBytes(msgType);

				// replace the bytes within the internal array
				Buffer.BlockCopy(msgTypeBytes, 0, writerBytes, 2, sizeof(short));

				((INetworkSender)this).PrepareAndSendWriter(writer);
			}

			base.OnEnterScope ();
		}

		NetworkWriter INetworkSender.CreateWriter(int signalType)
		{
			NetworkWriter writer = new NetworkWriter();

			writer.StartMessage(msgType);
			writer.Write(signalType);
			return writer;
		}

		void INetworkSender.PrepareAndSendWriter(NetworkWriter writer)
		{
			// only send the writer if the Scope is active
			if (IsActive)
			{
				writer.FinishMessage();

				#if UNITY_EDITOR && SCOPE_DEBUGGING
				// log outgoing signal
				ScopeDebugger.AddOutgoingSignal (this, typeof(TServerScope), new NetworkReader (writer.ToArray ()));
				#endif

				byte error;
				NetworkTransport.Send(client.connection.hostId, client.connection.connectionId, 0, writer.ToArray(), writer.Position, out error);

				if ((NetworkError)error != NetworkError.Ok)
					UnityEngine.Debug.LogError((NetworkError)error);
			}
			// otherwise, ignore or queue the Signal for later
			else
			{
				// if Signal queuing is enabled, enqueue the writer to be sent later
				if (_signalQueueEnabled)
					queuedSignalWriters.Enqueue(writer);
				// otherwise, just display a warning message
				else
					UnityEngine.Debug.LogWarningFormat("Ignoring Signal sending because the Scope <color=white>{0}</color> is no longer active", GetType().Name);
			}
		}
	}

}
