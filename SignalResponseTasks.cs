
using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace NetworkScopes
{
	public class PeerSignalResponseTasks<TPeer> where TPeer : NetworkPeer
	{
		public Dictionary<TPeer, SignalResponseTasks> peerResponseTasks;

		public NetworkTask<T> EnqueueResponseTask<T>(TPeer peer, int signalType)
		{
			if (peerResponseTasks == null)
				peerResponseTasks = new Dictionary<TPeer, SignalResponseTasks>();

			return GetPeerResponseTasks(peer).EnqueueResponseTask<T>(signalType);
		}

		SignalResponseTasks GetPeerResponseTasks(TPeer peer)
		{
			SignalResponseTasks responseTasks;
			if (!peerResponseTasks.TryGetValue(peer, out responseTasks))
			{
				peerResponseTasks[peer] = responseTasks = new SignalResponseTasks();

				peer.OnDisconnect += netPeer => peerResponseTasks.Remove(peer);
			}

			return responseTasks;
		}

		public void DequeueResponseObject<T>(TPeer peer, int signalType, NetIncomingMessage reader, Action<T, NetIncomingMessage> serializeMethod) where T : new()
		{
			GetPeerResponseTasks(peer).DequeueResponseObject(signalType, reader, serializeMethod);
		}

		public void DequeueResponseValue<T>(TPeer peer, int signalType, NetIncomingMessage reader, Func<NetIncomingMessage, T> deserializeMethod)
		{
			GetPeerResponseTasks(peer).DequeueResponseValue(signalType, reader, deserializeMethod);
		}
	}

	public class SignalResponseTasks
	{
		private Dictionary<int, Queue<NetworkTask>> tasks;

		public NetworkTask<T> EnqueueResponseTask<T>(int signalType)
		{
			if (tasks == null)
				tasks = new Dictionary<int, Queue<NetworkTask>>();

			Queue<NetworkTask> taskList;
			if (!tasks.TryGetValue(signalType, out taskList))
				tasks[signalType] = taskList = new Queue<NetworkTask>(1);	// allocate list for only 1 task per user per signal

			// create the task
			NetworkTask<T> task = new NetworkTask<T>();
			taskList.Enqueue(task);
			return task;
		}

		private void DequeueResponseTask<T>(int signalType, T value)
		{
			Queue<NetworkTask> networkTasks = tasks[signalType];
			NetworkTask<T> task = (NetworkTask<T>)networkTasks.Dequeue();
			task.OnCompleted(value);
		}

		public void DequeueResponseObject<T>(int signalType, NetIncomingMessage reader, Action<T,NetIncomingMessage> serializeMethod) where T : new()
		{
			T value = new T();
			serializeMethod(value, reader);

			DequeueResponseTask(signalType, value);
		}

		public void DequeueResponseValue<T>(int signalType, NetIncomingMessage reader, Func<NetIncomingMessage,T> deserializeMethod)
		{
			T value = deserializeMethod(reader);

			DequeueResponseTask(signalType, value);
		}
	}
}