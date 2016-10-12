
namespace NetworkScopes
{
	using UnityEngine;
	using UnityEngine.Networking;
	using System.Collections.Generic;
	using System.Collections;
	using System.IO;
	using System.Linq;
	using System;
	using System.Reflection;


	public abstract class BaseScope
	{
		public short msgType { get; protected set; }
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

		public void ProcessMessage (NetworkMessage msg)
		{
			#if UNITY_EDITOR && SCOPE_DEBUGGING
			// log incoming signal
			ScopeDebugger.AddIncomingSignal (this, msg.reader);

			msg.reader.SeekZero();
			#endif

			// if not pause, invoke the signal immediately
			if (!_isPaused)
			{
				MethodBindingCache.Invoke(this, cachedType, msg.reader);
			}
			// if paused, enqueue paused signal for later processing
			else
			{
				SignalInvocation inv = MethodBindingCache.GetMessageInvocation(cachedType, msg.reader);
				pausedMessages.Enqueue(inv);
			}
		}
	}
}
