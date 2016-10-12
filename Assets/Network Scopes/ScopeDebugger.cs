#if UNITY_EDITOR
using UnityEngine.Networking;
using System.Text;


namespace NetworkScopesV2
{
	using System;
	using UnityEditor;
	using System.Collections.Generic;
	using System.Reflection;
	using UnityEngine;
	using System.Linq;

	public static class ScopeDebugger
	{
		public struct ScopeEvents
		{
			public ScopeEvents(BaseScope scope)
			{
				this.scope = scope;

				signals = new List<SignalEvent>(100);
				events = new List<ScopeEvent>(10);
			}

			public BaseScope scope { get; private set; }

			public List<SignalEvent> signals { get; private set; }
			public List<ScopeEvent> events { get; private set; }

			public SignalEvent AddSignalEvent(MethodInfo method, NetworkReader reader, bool outgoing)
			{
				signals.Add(new SignalEvent(method, reader, outgoing));
				return signals[signals.Count-1];
			}

			public ScopeEvent AddScopeEvent(ScopeEvent.Type eventType, BaseScope scope1, BaseScope scope2)
			{
				events.Add(new ScopeEvent(eventType, scope1, scope2));
				return events[events.Count-1];
			}
		}

		public struct ScopeEvent
		{
			public enum Type
			{
				Enter,
				Exit,
				Switch,
			}

			public Type type;
			public BaseScope scope1;
			public BaseScope scope2;

			public DateTime receiveTime { get; private set; }

			public ScopeEvent(Type eventType, BaseScope scope1, BaseScope scope2)
			{
				type = eventType;

				this.scope1 = scope1;
				this.scope2 = scope2;

				receiveTime = DateTime.Now;
			}
		}

		public struct SignalEvent
		{
			public MethodInfo method { get; private set; }
			public string[] parameters { get; private set; }

			public DateTime receiveTime { get; private set; }

			public bool isOutgoing { get; private set; }

			public SignalEvent(MethodInfo method, NetworkReader reader, bool outgoing)
			{
				this.method = method;
				this.isOutgoing = outgoing;

				receiveTime = DateTime.Now;
				parameters = new string[0];

				if (method != null)
					SetParameters(method.GetParameters(), reader);
			}

			void SetParameters(ParameterInfo[] pi, NetworkReader reader)
			{
				parameters = new string[pi.Length];

				for (int x = 0; x < pi.Length; x++)
					parameters[x] = ReadParameterToString(pi[x].ParameterType, reader);
			}

			string ReadParameterToString(Type t, NetworkReader reader)
			{
				if (t.IsArray)
				{
					Type elemType = t.GetElementType();

					int count = reader.ReadInt32();

					StringBuilder sb = new StringBuilder();

					sb.Append("[");
					for (int x = 0; x < count; x++)
					{
						if (x != 0)
							sb.Append(", ");
						
						sb.Append(ReadParameterToString(elemType, reader));
					}
					sb.Append("]");

					return sb.ToString();
				}
				else
				{
					if (t == typeof(int))
						return reader.ReadInt32().ToString();
					else if (t == typeof(string))
						return reader.ReadString();
					else if (t == typeof(short))
						return reader.ReadInt16().ToString();
					else if (t == typeof(byte))
						return reader.ReadByte().ToString();
					else if (t.IsEnum)
					{
						return Enum.Parse(t, reader.ReadByte().ToString()).ToString();
					}
					else
					{
						MethodInfo deserializeMethod = t.GetMethod("NetworkDeserialize", BindingFlags.Static | BindingFlags.Public);

						if (deserializeMethod != null)
						{
							object value = Activator.CreateInstance(t);
							deserializeMethod.Invoke(null, new object[] { value, reader });

							return value.ToString();
						}
						else
							return string.Format("[{0}]", t.Name);
					}
				}
			}
		}

		public static List<ScopeEvents> scopeEventsList = null;

		public static bool IsEnabled { get { return scopeEventsList != null; } }

		public static event Action<BaseScope,SignalEvent> OnSignalEvent = null;
		public static event Action<BaseScope,BaseScope,ScopeEvent> OnScopeEvent = null;

		private static ScopeEvents GetOrCreateScopeEvent(BaseScope scope)
		{
			if (scopeEventsList == null)
			{
				scopeEventsList = new List<ScopeEvents>();
			}

			// find event
			int scopeEventIndex = scopeEventsList.FindIndex(s => s.scope == scope);

			// create and add if not found
			if (scopeEventIndex == -1)
			{
				ScopeEvents ev = new ScopeEvents(scope);
				scopeEventsList.Add(ev);
				return ev;
			}

			return scopeEventsList[scopeEventIndex];
		}

		public static void AddScopeEvent(BaseScope scope, BaseScope otherScope, ScopeEvent.Type scopeEv)
		{
			ScopeEvents ev = GetOrCreateScopeEvent(scope);

			ScopeEvent scopeEvent = ev.AddScopeEvent(scopeEv, scope, otherScope);

			if (OnScopeEvent != null)
				OnScopeEvent(scope, otherScope, scopeEvent);
		}

		public static void AddIncomingSignal(BaseScope scope, NetworkReader reader)
		{
			int signalType = reader.ReadInt32();

			MethodInfo method = scope.GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(m => m.Name.GetConsistentHashCode() == signalType);

			ScopeEvents ev = GetOrCreateScopeEvent(scope);

			SignalEvent sigEvent = ev.AddSignalEvent(method, reader, false);

			reader.SeekZero();

			if (OnSignalEvent != null)
				OnSignalEvent(scope, sigEvent);
		}

		public static void AddOutgoingSignal(BaseScope scope, Type outgoingScopeType, NetworkReader reader)
		{
			// read useless data
			reader.ReadInt32();

			int signalType = reader.ReadInt32();

			MethodInfo method = outgoingScopeType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(m => m.Name.GetConsistentHashCode() == signalType);

			if (method == null)
				Debug.Log("Could not find method with sigType " + signalType);

			ScopeEvents ev = GetOrCreateScopeEvent(scope);

			SignalEvent sigEvent = ev.AddSignalEvent(method, reader, false);

			reader.SeekZero();

			if (OnSignalEvent != null)
				OnSignalEvent(scope, sigEvent);
		}

		public static void Dispose()
		{
			scopeEventsList = null;

			OnSignalEvent = null;
			OnScopeEvent = null;
		}
	}
	
}
#endif