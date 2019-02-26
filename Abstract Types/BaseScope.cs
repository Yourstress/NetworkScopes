
using Lidgren.Network;


namespace NetworkScopes
{
	using System.Collections.Generic;
	using System;

	public interface IScope
	{
		short msgType { get; }
		byte scopeIdentifier { get; }
	}

	public abstract class BaseScope : IMessageSender, IScope
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

			InitializePartialScope();
		}

		protected virtual void InitializePartialScope()
		{
		}

		public void SetScopeIdentifier(byte identifier)
		{
			scopeIdentifier = identifier;
		}

		public void ProcessSignal(IncomingNetworkPacket packet)
		{
			// if not pause, invoke the signal immediately
			if (!_isPaused)
			{
				MethodBindingCache.Invoke(this, cachedType, packet);
			}
			// if paused, enqueue paused signal for later processing
			else
			{
				SignalInvocation inv = MethodBindingCache.GetMessageInvocation(cachedType, packet);
				pausedMessages.Enqueue(inv);
			}
		}

		public OutgoingNetworkPacket CreatePacket(int signalType)
		{
			// TODO: implement packet pooling
			OutgoingNetworkPacket packet = NetworkPacket.CreateOutgoingPacket(msgType, signalType);
			return packet;
		}

		public abstract void SendPacket(NetworkPacket packet);
	}
}
