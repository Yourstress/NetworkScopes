
#define NS_DEBUG_SCOPE_ACTIVITY

using Lidgren.Network;

namespace NetworkScopes
{
	using System;

	public enum ScopeEvent
	{
		Enter,
		Exit
	}

	public abstract class BaseClientScope : BaseScope, IDisposable
	{
		protected NetClient client;

		public MasterClient MasterClient { get; private set; }

		public bool IsActive { get; private set; }
		public bool FlushScopeEventsOnDispose = false;

		public void Initialize(short scopeMsgType, NetClient currentClient, MasterClient masterClient)
		{
			msgType = scopeMsgType;

			client = currentClient;
			MasterClient = masterClient;

			base.Initialize();
		}

		#region IDisposable implementation
		public virtual void Dispose ()
		{
			client = null;

			if (FlushScopeEventsOnDispose)
			{
				OnEnterScopeEvent = delegate {};
				OnExitScopeEvent = delegate {};
			}
		}
		#endregion

		public void EnterScope(NetIncomingMessage netVariableData)
		{
			IsActive = true;
			
			#if NS_DEBUG_SCOPE_ACTIVITY
            NetworkDebug.LogFormat("Entered Scope {0}", GetType().Name);
#endif

#if UNITY_EDITOR && SCOPE_DEBUGGING
			ScopeDebugger.AddScopeEvent(this, null, ScopeDebugger.ScopeEvent.Type.Enter);
#endif
			OnEnterScope();

			OnEnterScopeEvent();

			OnScopeEvent(ScopeEvent.Enter);
		}

		public void ExitScope()
		{
			#if NS_DEBUG_SCOPE_ACTIVITY
            NetworkDebug.LogFormat("Exited Scope {0}", GetType().Name);
			#endif

			#if UNITY_EDITOR && SCOPE_DEBUGGING
			ScopeDebugger.AddScopeEvent(this, null, ScopeDebugger.ScopeEvent.Type.Exit);
			#endif

			OnExitScope();

			OnExitScopeEvent();

			IsActive = false;

			OnScopeEvent(ScopeEvent.Exit);
		}

		public event Action OnEnterScopeEvent = delegate {};
		public event Action OnExitScopeEvent = delegate {};
		public event Action<ScopeEvent> OnScopeEvent = delegate {};

		protected virtual void OnEnterScope()
		{
		}

		public virtual void OnExitScope()
		{
		}
	}
}
