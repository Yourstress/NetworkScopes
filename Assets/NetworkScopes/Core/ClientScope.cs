// #define LOG_SCOPE_EVENTS

using System;
using System.Collections.Generic;

namespace NetworkScopes
{
	public abstract class ClientScope<TScopeSender> : IClientScope where TScopeSender : IScopeSender
	{
		public string name => GetType().Name;
		public bool IsActive { get; private set; }
		public bool FlushScopeEventsOnDispose = false;

		protected abstract TScopeSender GetScopeSender();

		public TScopeSender SendToServer => GetScopeSender();

		private IClientSignalProvider _signalProvider;

		public ScopeIdentifier scopeIdentifier { get; private set; }
		public ScopeChannel channel { get; private set; }
		
		public event Action OnEnterScopeEvent = delegate { };
		public event Action OnExitScopeEvent = delegate { };

		private readonly NetworkPromiseHandler promiseHandler = new NetworkPromiseHandler();
		
		#region Signal Queuing
		private bool _signalQueueEnabled = false;
		private Queue<SignalWriter> _queuedSignals;

		public bool SignalQueueEnabled
		{
			get => _signalQueueEnabled;
			set
			{
				_signalQueueEnabled = value;

				if (value)
					_queuedSignals = new Queue<SignalWriter>(3);
				else
					_queuedSignals = null;
			}
		}
		#endregion

		void IClientScope.Initialize(IClientSignalProvider serviceProvider, ScopeIdentifier scopeIdentifier)
		{
			this.scopeIdentifier = scopeIdentifier;

			_signalProvider = serviceProvider;

			SignalMethodBinder.BindScope(this);
		}

		protected ISignalWriter CreateSignal(int signalID)
		{
			// return writer based on the current network medium (service provider)
			ISignalWriter signal = _signalProvider.CreateSignal(channel);
			signal.Write(signalID);
			return signal;
		}

		protected ISignalWriter CreatePromiseSignal(int signalID, INetworkPromise promise)
		{
			ISignalWriter signal = CreateSignal(signalID);
			signal.Write(promiseHandler.EnqueuePromise(promise));
			return signal;
		}

		protected void SendSignal(ISignalWriter signal)
		{
			// send out the signal using the service provider
			_signalProvider.SendSignal(signal);
		}

		protected void ReceivePromise(ISignalReader reader)
		{
			int promiseID = reader.ReadInt32();

			promiseHandler.DequeueAndReceivePromise(promiseID, reader);
		}

		protected virtual void OnEnterScope()
		{
			#if LOG_SCOPE_EVENTS
			Debug.Log("[ENTERED] " + GetType().Name);
			#endif
			
			OnEnterScopeEvent();
			
			// right after entering the scope, send out all queued signals
			ProcessQueuedSignals();
		}

		protected virtual void OnExitScope()
		{
			#if LOG_SCOPE_EVENTS
			Debug.Log("[EXITED] " + GetType().Name);
			#endif
			
			OnExitScopeEvent();
		}

		private void ProcessQueuedSignals()
		{
			// if queueing enabled, send out all queued signals one by one
			while (_signalQueueEnabled && _queuedSignals.Count > 0)
			{
				SignalWriter signal = _queuedSignals.Dequeue();

				// modify the 2nd and 3rd bytes in the array containing the channel (channel is only correct after entering the scope)
				byte[] writerBytes = signal.Data;
				byte[] channelBytes = BitConverter.GetBytes(channel);

				// replace the bytes within the internal array
				Buffer.BlockCopy(channelBytes, 0, writerBytes, 2, sizeof(short));
				
				// send if out
				SendSignal(signal);
			}
		}

		void IClientScope.EnterScope(ScopeChannel channel)
		{
			if (IsActive)
				throw new Exception("Failed to enter active scope.");

			this.channel = channel;
			IsActive = true;

			OnEnterScope();
		}

		void IClientScope.ExitScope()
		{
			if (!IsActive)
				throw new Exception("Failed to exit inactive scope.");

			IsActive = false;

			promiseHandler.ClearPromises();

			OnExitScope();
		}

		void IClientScope.ProcessSignal(ISignalReader signal)
		{
			SignalMethodBinder.Invoke(this, GetType(), signal);
		}

		public virtual void Dispose()
		{
			if (FlushScopeEventsOnDispose)
			{
				OnEnterScopeEvent = delegate {};
				OnExitScopeEvent = delegate {};
			}
		}
	}
}