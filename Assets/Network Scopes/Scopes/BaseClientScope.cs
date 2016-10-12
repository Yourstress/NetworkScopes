
//#define NS_DEBUG_SCOPE_ACTIVITY

namespace NetworkScopes
{
	using System;
	using UnityEngine.Networking;

	public abstract class BaseClientScope : BaseScope, IDisposable
	{
		protected NetworkClient client;

		public MasterClient MasterClient { get; private set; }

		public bool IsActive { get; private set; }
		public bool FlushScopeEventsOnDispose = true;

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

			if (FlushScopeEventsOnDispose)
			{
				OnEnterScopeEvent = delegate {};
				OnExitScopeEvent = delegate {};
			}
		}
		#endregion

		public void EnterScope()
		{
			IsActive = true;
			
			#if NS_DEBUG_SCOPE_ACTIVITY
			UnityEngine.Debug.LogFormat("<color=green>Entered</color> Scope <color=white>{0}</color>", GetType().Name);
			#endif

			#if UNITY_EDITOR && SCOPE_DEBUGGING
			ScopeDebugger.AddScopeEvent(this, null, ScopeDebugger.ScopeEvent.Type.Enter);
			#endif

			OnEnterScope();

			OnEnterScopeEvent();
		}

		public void ExitScope()
		{
			#if NS_DEBUG_SCOPE_ACTIVITY
			UnityEngine.Debug.LogFormat("<color=red>Exited</color> Scope <color=white>{0}</color>", GetType().Name);
			#endif

			#if UNITY_EDITOR && SCOPE_DEBUGGING
			ScopeDebugger.AddScopeEvent(this, null, ScopeDebugger.ScopeEvent.Type.Exit);
			#endif

			OnExitScope();

			OnExitScopeEvent();

			IsActive = false;
		}

		public event Action OnEnterScopeEvent = delegate {};
		public event Action OnExitScopeEvent = delegate {};

		protected virtual void OnEnterScope()
		{
		}

		public virtual void OnExitScope()
		{
		}
	}
}
