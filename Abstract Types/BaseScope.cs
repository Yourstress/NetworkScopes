
using Lidgren.Network;


namespace NetworkScopes
{
	using System.Collections.Generic;
	using System;

	public interface IScope
	{
		short msgType { get; }
		byte scopeIdentifier { get; }

		void ReadNetworkVariables(NetIncomingMessage reader);
		void WriteNetworkVariables(NetOutgoingMessage writer);
	}

	public abstract class BaseScope : IMessageSender, INetworkVariableSender, IScope
	{
		public short msgType { get; protected set; }
		public byte scopeIdentifier { get; protected set; }

		private Type cachedType;

		private bool _isPaused = false;
		private Queue<SignalInvocation> pausedMessages = null;

		public bool IsPaused
		{
			get { return _isPaused; }
			set
			{
				if (_isPaused == value)
					return;
				
				_isPaused = value;

				// initialize list when pausing is enabled
				if (_isPaused && pausedMessages == null)
					pausedMessages = new Queue<SignalInvocation>(10);
				
				// while unpaused, process messages in the queue
				while (!_isPaused && pausedMessages.Count > 0)
				{
					SignalInvocation pausedInv = pausedMessages.Dequeue();
					pausedInv.Invoke(this);
				}
			}
		}

		protected void Initialize()
		{
			cachedType = GetType();
			MethodBindingCache.BindScope(cachedType);

			InitializePartialScope();
		}

		protected virtual void InitializePartialScope()
		{
		}

		public void SetScopeIdentifier(byte identifier)
		{
			scopeIdentifier = identifier;
		}

		public void ProcessSignal(NetIncomingMessage msg)
		{
			// if not pause, invoke the signal immediately
			if (!_isPaused)
			{
				MethodBindingCache.Invoke(this, cachedType, msg);
			}
			// if paused, enqueue paused signal for later processing
			else
			{
				SignalInvocation inv = MethodBindingCache.GetMessageInvocation(cachedType, msg);
				pausedMessages.Enqueue(inv);
			}
		}

		public abstract NetOutgoingMessage CreateWriter(int signalType);
		public abstract void PrepareAndSendWriter(NetOutgoingMessage msg);
		public abstract void SendNetworkVariableWriter(NetOutgoingMessage msg);

		#region NetworkVariable Tracking
		private List<INetworkVariable> trackedNetworkVariables;

		protected void TrackNetworkVariable(INetworkVariable networkVariable)
		{
			if (trackedNetworkVariables == null)
				trackedNetworkVariables = new List<INetworkVariable>();
			trackedNetworkVariables.Add(networkVariable);
		}

		protected void UntrackNetworkVariable(INetworkVariable networkVariable)
		{
			trackedNetworkVariables?.Remove(networkVariable);
		}

		void IScope.ReadNetworkVariables(NetIncomingMessage reader)
		{
			if (trackedNetworkVariables == null)
				return;

			while (reader.PositionInBytes < reader.LengthBytes)
			{
				int objId = reader.ReadInt32();

				INetworkVariable networkVariable = trackedNetworkVariables.Find(v => objId == v.objectId);

				if (networkVariable == null)
				{
					NetworkDebug.LogError($"Could not find NetworkVariable (id={objId}) in the scope '{GetType().Name}'.");
					return;
				}

				networkVariable.Read(reader);
			}
		}

		void IScope.WriteNetworkVariables(NetOutgoingMessage writer)
		{
			if (trackedNetworkVariables != null)
			{
				// write each network variable's objectID and data in a serial fashion
				for (int x = 0; x < trackedNetworkVariables.Count; x++)
				{
					writer.Write(trackedNetworkVariables[x].objectId);
					trackedNetworkVariables[x].Write(writer);
				}
			}
		}
		#endregion

		#region Network Object Registration
		protected void RegisterNetworkObject<T>(ref NetworkObject<T> networkObject, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod) where T : class, new()
		{
			// initialize it if needed
			if (networkObject == null)
				networkObject = new NetworkObject<T>();

			// then register it normally
			RegisterNetworkObject(networkObject, objectId, serializeMethod, deserializeMethod);
		}

		protected void RegisterNetworkObject<T>(ref INetworkObject<T> networkObject, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod) where T : class, new()
		{
			// initialize it if needed
			if (networkObject == null)
				networkObject = new NetworkObject<T>();

			// then register it normally
			RegisterNetworkObject(networkObject, objectId, serializeMethod, deserializeMethod);
		}

		protected void RegisterNetworkObject<T>(INetworkObject<T> networkVariable, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod) where T : class, new()
		{
			// initialize it by telling it its ID and how to serialize/deserialize itself
			networkVariable.Initialize(this, objectId, serializeMethod, deserializeMethod);

			TrackNetworkVariable(networkVariable);
		}

		protected void RegisterNetworkList<T>(ref NetworkList<T> networkList, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod) where T : class, new()
		{
			// initialize it if needed
			if (networkList == null)
				networkList = new NetworkList<T>();

			// then register it normally
			RegisterNetworkList(networkList, objectId, serializeMethod, deserializeMethod);
		}

		protected void RegisterNetworkList<T>(INetworkList<T> networkList, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod) where T : class, new()
		{
			networkList.Initialize(this, objectId, serializeMethod, deserializeMethod);

			TrackNetworkVariable(networkList);
		}
		#endregion

		#region Network Value Registration
		protected void RegisterNetworkValue<T>(ref NetworkValue<T> networkObject, int objectId, NetworkSerialization.SerializeValueDelegate<T> serializeMethod, NetworkSerialization.DeserializeValueDelegate<T> deserializeMethod)
		{
			// initialize it if needed
			if (networkObject == null)
				networkObject = new NetworkValue<T>();

			// then register it normally
			RegisterNetworkValue(networkObject, objectId, serializeMethod, deserializeMethod);
		}

		protected void RegisterNetworkValue<T>(ref INetworkValue<T> networkObject, int objectId, NetworkSerialization.SerializeValueDelegate<T> serializeMethod, NetworkSerialization.DeserializeValueDelegate<T> deserializeMethod)
		{
			// initialize it if needed
			if (networkObject == null)
				networkObject = new NetworkValue<T>();

			// then register it normally
			RegisterNetworkValue(networkObject, objectId, serializeMethod, deserializeMethod);
		}

		protected void RegisterNetworkValue<T>(INetworkValue<T> networkVariable, int objectId, NetworkSerialization.SerializeValueDelegate<T> serializeMethod, NetworkSerialization.DeserializeValueDelegate<T> deserializeMethod)
		{
			// initialize it by telling it its ID and how to serialize/deserialize itself
			networkVariable.Initialize(this, objectId, serializeMethod, deserializeMethod);

			TrackNetworkVariable(networkVariable);
		}
		#endregion
	}
}
