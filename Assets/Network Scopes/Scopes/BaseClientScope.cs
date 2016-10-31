
#define NS_DEBUG_SCOPE_ACTIVITY

namespace NetworkScopes
{
	using System;

	public abstract class BaseClientScope : BaseScope, IDisposable
	{
		public BaseClient Client { get; private set; }

		public bool IsActive { get; private set; }
		public bool FlushScopeEventsOnDispose = true;

		public void Initialize(short scopeMsgType, BaseClient client)
		{
			scopeChannel = scopeMsgType;

			Client = client;

			// register with the Client's Scope msgType
			Client.RegisterScopeHandler(scopeChannel, ProcessMessage);

			base.Initialize();
		}

		#region IDisposable implementation
		public virtual void Dispose ()
		{
			// unregister the msgType from the client
			Client.UnregisterScopeHandler(scopeChannel);

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
			ScopeUtils.Log("<color=green>Entered</color> Scope <color=white>{0}</color>", GetType().Name);
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
			ScopeUtils.Log("<color=red>Exited</color> Scope <color=white>{0}</color>", GetType().Name);
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
