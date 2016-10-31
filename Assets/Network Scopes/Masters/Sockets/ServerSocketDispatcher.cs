
#if UNITY_5_3_OR_NEWER
namespace NetworkScopes
{
	using UnityEngine;
	using System.Collections.Generic;
	using System;
	using System.Net.Sockets;

	public class ServerSocketDispatcher : MonoBehaviour
	{
		public delegate void ReceiveSignalDelegate (IMessageReader reader);

		struct ReceiveSignalStruct
		{
			public IMessageReader reader;
			public TcpClient sender;
			public Action<IMessageReader,TcpClient> scopeHandler;
		}

		private object queueLock = new object ();
		private Queue<ReceiveSignalStruct> queuedSignals = new Queue<ReceiveSignalStruct> ();

		private static ServerSocketDispatcher _instance = null;

		public static void TryInitialize ()
		{
			if (_instance == null) {
				_instance = new GameObject ("ClientSocketDispatcher").AddComponent<ServerSocketDispatcher> ();
				_instance.gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
		}

		public static void EnqueueMessage (IMessageReader reader, TcpClient sender, Action<IMessageReader,TcpClient> scopeHandler)
		{
			lock (_instance.queueLock) {
				ReceiveSignalStruct msg = new ReceiveSignalStruct () {
					reader = reader,
					sender = sender,
					scopeHandler = scopeHandler
				};
				_instance.queuedSignals.Enqueue (msg);
			}
		}

		void Update ()
		{
			lock (queueLock) {
				while (queuedSignals.Count > 0) {
					ReceiveSignalStruct msg = queuedSignals.Dequeue ();

					// call the delegate with the reader as a parameter
					msg.scopeHandler (msg.reader, msg.sender);
				}
			}
		}
	}
}
#endif