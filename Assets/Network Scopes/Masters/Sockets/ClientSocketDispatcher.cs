
#if UNITY_5_3_OR_NEWER
namespace NetworkScopes
{
	using UnityEngine;
	using System.Collections.Generic;
	using System;

	public class ClientSocketDispatcher : MonoBehaviour
	{
		public delegate void ReceiveSignalDelegate (IMessageReader reader);

		struct ReceiveSignalStruct
		{
			public IMessageReader reader;
			public BaseClient.ScopeHandlerDelegate scopeHandler;
		}

		private object queueLock = new object ();
		private Queue<ReceiveSignalStruct> queuedSignals = new Queue<ReceiveSignalStruct> ();
		private Queue<Action> queuedActions = new Queue<Action> ();

		private static ClientSocketDispatcher _instance = null;

		public static void TryInitialize ()
		{
			if (_instance == null) {
				_instance = new GameObject ("ClientSocketDispatcher").AddComponent<ClientSocketDispatcher> ();
				_instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
		}

		public static void AddReceivedMessage (IMessageReader reader, BaseClient.ScopeHandlerDelegate scopeHandler)
		{
			lock (_instance.queueLock) {
				_instance.queuedSignals.Enqueue (new ReceiveSignalStruct () { reader = reader, scopeHandler = scopeHandler });
			}
		}

		public static void QueueAction (Action action)
		{
			lock (_instance.queueLock) {
				_instance.queuedActions.Enqueue (action);
			}
		}

		void Update ()
		{
			lock (queueLock) {
				while (queuedSignals.Count > 0) {
					ReceiveSignalStruct msg = queuedSignals.Dequeue ();

					// call the delegate with the reader as a parameter
					msg.scopeHandler (msg.reader);
				}

				while (queuedActions.Count > 0)
					queuedActions.Dequeue () ();
			}
		}
	}
}
#endif