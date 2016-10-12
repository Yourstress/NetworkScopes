
namespace NetworkScopesV2
{
	using System;
	using System.Collections.Generic;

	public abstract class BaseScope
	{
		public short scopeChannel { get; protected set; }
		public byte scopeIdentifier { get; protected set; }

		private Type cachedType;

		private bool _isPaused = false;
		private Queue<SignalInvocation> pausedMessages = null;

		public bool IsPaused
		{
			get { return _isPaused; }
			set
			{
				if (_isPaused == value)
					return;
				
				_isPaused = value;

				// initialize list when pausing is enabled
				if (_isPaused && pausedMessages == null)
					pausedMessages = new Queue<SignalInvocation>(10);
				
				// while unpaused, process messages in the queue
				while (!_isPaused && pausedMessages.Count > 0)
				{
					SignalInvocation pausedInv = pausedMessages.Dequeue();
					pausedInv.Invoke(this);
				}
			}
		}

		protected void Initialize()
		{
			cachedType = GetType();
			MethodBindingCache.BindScope(cachedType);
		}

		public void SetScopeIdentifier(byte identifier)
		{
			scopeIdentifier = identifier;
		}

		public void ProcessMessage (IMessageReader msgReader)
		{
			#if UNITY_EDITOR && SCOPE_DEBUGGING
			// log incoming signal
			ScopeDebugger.AddIncomingSignal (this, msg.reader);

			msg.reader.SeekZero();
			#endif

			// if not pause, invoke the signal immediately
			if (!_isPaused)
			{
				MethodBindingCache.Invoke(this, cachedType, msgReader);
			}
			// if paused, enqueue paused signal for later processing
			else
			{
				SignalInvocation inv = MethodBindingCache.GetMessageInvocation(cachedType, msgReader);
				pausedMessages.Enqueue(inv);
			}
		}
	}
}
