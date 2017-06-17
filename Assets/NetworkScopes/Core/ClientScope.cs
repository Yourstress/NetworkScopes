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

		private IClientSignalProvider _serviceProvider;

		public ScopeIdentifier scopeIdentifier { get; private set; }
		public ScopeChannel currentChannel { get; private set; }

		void IClientScope.Initialize(IClientSignalProvider serviceProvider, ScopeIdentifier scopeIdentifier)
		{
			this.scopeIdentifier = scopeIdentifier;

			_serviceProvider = serviceProvider;

			SignalMethodBinder.BindScope(GetType());
		}

		// TODO: pass down signal options (which scope and which signal method)
		protected ISignalWriter CreateSignal(int signalID)
		{
			// return writer based on the current network medium (service provider)
			ISignalWriter signal = _serviceProvider.CreateSignal(currentChannel);
			signal.WriteInt32(signalID);
			return signal;
		}

		protected void SendSignal(ISignalWriter signal)
		{
			// send out the signal using the service provider
			_serviceProvider.SendSignal(signal);
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
			// TODO: read the signal id and find the method associated

			// TODO: read parameters from the reader based on the expected methods

			// TODO: call method with parameters
		}
	}
}