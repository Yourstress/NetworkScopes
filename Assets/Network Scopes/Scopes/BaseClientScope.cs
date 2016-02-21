
#define NS_DEBUG_SCOPE_ACTIVITY

namespace NetworkScopes
{
	using System;
	using UnityEngine.Networking;

	public abstract class BaseClientScope : BaseScope, IDisposable
	{
		protected NetworkClient client;

		public MasterClient MasterClient { get; private set; }

		public bool IsActive { get; private set; }

		public void Initialize(short scopeMsgType, NetworkClient currentClient, MasterClient masterClient)
		{
			msgType = scopeMsgType;

			client = currentClient;
			MasterClient = masterClient;

			// register with the Client's Scope msgType
			client.RegisterHandler(msgType, ProcessMessage);

			base.Initialize();
		}

		#region IDisposable implementation
		public virtual void Dispose ()
		{
			// unregister the msgType from the client
			client.UnregisterHandler(msgType);

			client = null;
		}
		#endregion

		public void EnterScope()
		{
			#if NS_DEBUG_SCOPE_ACTIVITY
			UnityEngine.Debug.LogFormat("<color=green>Entered</color> Scope <color=white>{0}</color>", GetType().Name);
			#endif
			
			IsActive = true;

			OnEnterScope();
		}

		public void ExitScope()
		{
			#if NS_DEBUG_SCOPE_ACTIVITY
			UnityEngine.Debug.LogFormat("<color=red>Exited</color> Scope <color=white>{0}</color>", GetType().Name);
			#endif

			OnExitScope();

			IsActive = false;
		}

		protected virtual void OnEnterScope()
		{
		}

		public virtual void OnExitScope()
		{
		}
	}
}
