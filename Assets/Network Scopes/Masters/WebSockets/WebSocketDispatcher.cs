
namespace NetworkScopes
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;

	public enum WSEventType
	{
		Open,
		Close,
		Error,
		Message,
	}

	public struct WSEvent
	{
		public WebSocketConnectionHandler connection;
		public WSEventType type;
		public byte[] data;

		public WSEvent(WebSocketConnectionHandler connection, WSEventType type, byte[] data = null)
		{
			this.connection = connection;
			this.type = type;
			this.data = data;
		}
	}

	public class WebSocketServerDispatcher : MonoBehaviour
	{
		private static bool isInitialized = false;
		private static WebSocketServerDispatcher instance = null;

		private object _eventLock = new object();
		private Queue<WSEvent> _events = new Queue<WSEvent>();
		private Action<WSEvent> _onEventReceived;

		public static void Initialize(Action<WSEvent> onEventReceived)
		{
			if (isInitialized)
				return;

			isInitialized = true;

			GameObject dispatcherGO = new GameObject("WebSocketServerDispatcher");
			dispatcherGO.hideFlags = HideFlags.HideInInspector;
			instance = dispatcherGO.AddComponent<WebSocketServerDispatcher>();
			instance._onEventReceived = onEventReceived;
		}

		public static void Enqueue(WSEvent ev)
		{
			lock (instance._eventLock)
			{
				instance._events.Enqueue(ev);
			}
		}

		void Update()
		{
			lock (_eventLock)
			{
				while (_events.Count > 0)
				{
					WSEvent ev = _events.Dequeue();
					_onEventReceived(ev);
				}
			}
		}
	}

	public class WebSocketClientDispatcher : MonoBehaviour
	{
		private static bool isInitialized = false;
		private static WebSocketClientDispatcher instance = null;

		private object _eventLock = new object();
		private Queue<ClientEvent> _events = new Queue<ClientEvent>();
		private Action<WSEventType,byte[]> _onEventReceived;

		struct ClientEvent
		{
			public WSEventType type;
			public byte[] data;
		}

		public static void Initialize(Action<WSEventType,byte[]> onEventReceived)
		{
			if (isInitialized)
				return;

			isInitialized = true;

			GameObject dispatcherGO = new GameObject("WebSocketClientDispatcher");
			dispatcherGO.hideFlags = HideFlags.HideInInspector;
			instance = dispatcherGO.AddComponent<WebSocketClientDispatcher>();
			instance._onEventReceived = onEventReceived;
		}

		public static void Enqueue(WSEventType type, byte[] data = null)
		{
			lock (instance._eventLock)
			{
				instance._events.Enqueue(new ClientEvent() { type = type, data = data });
			}
		}

		void Update()
		{
			lock (_eventLock)
			{
				while (_events.Count > 0)
				{
					ClientEvent ev = _events.Dequeue();
					_onEventReceived(ev.type, ev.data);
				}
			}
		}
	}
}