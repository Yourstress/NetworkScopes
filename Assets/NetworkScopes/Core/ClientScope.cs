using System;
using UnityEngine;

namespace NetworkScopes
{
	public abstract class ClientScope<TScopeSender> : IClientScope where TScopeSender : IScopeSender
	{
		public string name { get { return GetType().Name; } }
		public bool isActive { get; private set; }

		protected abstract TScopeSender GetScopeSender();

		public TScopeSender SendToServer
		{
			get { return GetScopeSender(); }
		}

		private IClientSignalProvider _signalProvider;

		public ScopeIdentifier scopeIdentifier { get; private set; }
		public ScopeChannel currentChannel { get; private set; }

		private NetworkPromiseHandler promiseHandler = new NetworkPromiseHandler();

		void IClientScope.Initialize(IClientSignalProvider serviceProvider, ScopeIdentifier scopeIdentifier)
		{
			this.scopeIdentifier = scopeIdentifier;

			_signalProvider = serviceProvider;

			SignalMethodBinder.BindScope(GetType());
		}

		protected ISignalWriter CreateSignal(int signalID)
		{
			// return writer based on the current network medium (service provider)
			ISignalWriter signal = _signalProvider.CreateSignal(currentChannel);
			signal.WriteInt32(signalID);
			return signal;
		}

		protected ISignalWriter CreatePromiseSignal(int signalID, INetworkPromise promise)
		{
			ISignalWriter signal = CreateSignal(signalID);
			signal.WriteInt32(promiseHandler.EnqueuePromise(promise));
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
			Debug.Log("[ENTERED] " + GetType().Name);
		}

		protected virtual void OnExitScope()
		{
			Debug.Log("[EXITED] " + GetType().Name);
		}

		void IClientScope.EnterScope(ScopeChannel channel)
		{
			if (isActive)
				throw new Exception("Failed to enter active scope.");

			currentChannel = channel;
			isActive = true;

			OnEnterScope();
		}

		void IClientScope.ExitScope()
		{
			if (!isActive)
				throw new Exception("Failed to exit inactive scope.");

			isActive = false;

			OnExitScope();
		}

		void IClientScope.ProcessSignal(ISignalReader signal)
		{
			SignalMethodBinder.Invoke(this, GetType(), signal);
		}
	}
}