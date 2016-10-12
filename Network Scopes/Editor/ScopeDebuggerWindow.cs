#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;


namespace NetworkScopesV2
{
	using UnityEngine;
	using UnityEditor;


	public class ScopeDebuggerWindow : EditorWindow
	{
		bool scrollToBottom;

		public ScopeDebuggerWindow() : base()
		{
			ScopeDebugger.OnScopeEvent += ScopeDebugger_OnScopeEvent;
			ScopeDebugger.OnSignalEvent += ScopeDebugger_OnSignalEvent;
		}

		~ScopeDebuggerWindow()
		{
			ScopeDebugger.OnScopeEvent -= ScopeDebugger_OnScopeEvent;
			ScopeDebugger.OnSignalEvent -= ScopeDebugger_OnSignalEvent;
		}

		struct Log
		{
			public string name;
			public Color color;
			public string[] parameters;

			public Log(BaseScope scope, ScopeDebugger.SignalEvent sigEv)
			{
				name = sigEv.method.Name;
				parameters = sigEv.parameters;

				if (sigEv.isOutgoing)
					color = Color.green;
				else
					color = Color.cyan;
			}

			public Log(BaseScope scope, BaseScope otherScope, ScopeDebugger.ScopeEvent scopeEv)
			{
				name = scopeEv.type.ToString() + " Scope";

				color = Color.white;
				if (scopeEv.type == ScopeDebugger.ScopeEvent.Type.Switch)
					parameters = new string[] { scope.GetType().Name, otherScope.GetType().Name };
				else
					parameters = new string[] { scope.GetType().Name };
			}
		}

		List<Log> logs = new List<Log>(256);

		void ScopeDebugger_OnSignalEvent (BaseScope scope, ScopeDebugger.SignalEvent signalEvent)
		{
			logs.Add( new Log(scope, signalEvent) );

			Repaint();
		}

		void ScopeDebugger_OnScopeEvent (BaseScope scope, BaseScope otherScope, ScopeDebugger.ScopeEvent scopeEvent)
		{
			logs.Add( new Log(scope, otherScope, scopeEvent) );

			Repaint();
		}
		
		void ScopeDebugger_OnChanged ()
		{
			scroll.y = Mathf.Infinity;

			Repaint();
		}
		[MenuItem("Network Scopes/Scope Debugger Window")]
		static void ShowWindow()
		{
			EditorWindow.GetWindow<ScopeDebuggerWindow>().titleContent = new GUIContent("Scope Debugger");
		}

		void OnGUI()
		{
			DrawTabSelection ();
		}

		private int currentScopeIndex = -1;
		
		void DrawTabSelection ()
		{
			// draw tab selection
			using (new EditorGUILayout.HorizontalScope ())
			{
				if (GUILayout.Toggle(currentScopeIndex == -1, "All", "ButtonLeft"))
				{
					currentScopeIndex = -1;
				}
				
				if (ScopeDebugger.IsEnabled)
				{
					for (int scopeIndex = 0; scopeIndex < ScopeDebugger.scopeEventsList.Count; scopeIndex++)
					{
						string scopeName = ScopeDebugger.scopeEventsList[scopeIndex].scope.GetType().Name;

						if (GUILayout.Toggle(currentScopeIndex == scopeIndex, scopeName, "ButtonMid"))
						{
							currentScopeIndex = scopeIndex;
						}

						GUILayout.Space(2);
					}
				}
			}
			
			if (ScopeDebugger.IsEnabled)
			{
				if (currentScopeIndex >= 0 && currentScopeIndex < ScopeDebugger.scopeEventsList.Count)
					DrawScopeMethods(ScopeDebugger.scopeEventsList[currentScopeIndex]);
				else
					DrawAllEvents();
			}
		}

		Vector2 scroll;

		void DrawAllEvents()
		{
			scroll = GUILayout.BeginScrollView(scroll);

			for (int x = 0; x < logs.Count; x++)
			{
				using (new GUILayout.HorizontalScope())
				{
					GUI.color = logs[x].color;

					GUILayout.Label(logs[x].name, "CN CountBadge");

					GUI.color = Color.white;

					GUILayout.FlexibleSpace();

					for (int p = 0; p < logs[x].parameters.Length; p++)
						GUILayout.Label(logs[x].parameters[p], "CN CountBadge");
				}
			}

			GUILayout.EndScrollView();
		}

		void DrawScopeMethods(ScopeDebugger.ScopeEvents ev)
		{
			scroll = GUILayout.BeginScrollView(scroll);
			for (int x = 0; x < ev.signals.Count; x++)
			{
				using (new GUILayout.HorizontalScope())
				{
					GUI.color = ev.signals[x].isOutgoing ? Color.cyan : Color.green;

					GUILayout.Label(ev.signals[x].method.Name, "CN CountBadge");

					GUI.color = Color.white;

					GUILayout.FlexibleSpace();

					for (int p = 0; p < ev.signals[x].parameters.Length; p++)
					{
						if (ev.signals[x].parameters[p] != null)
							GUILayout.Label(ev.signals[x].parameters[p].ToString(), "CN CountBadge");
					}

					GUILayout.FlexibleSpace();
				}
			}
			GUILayout.EndScrollView();
		}
	}
}
#endif