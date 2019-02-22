
namespace NetworkScopes
{
	using System.Collections;
	using System.Collections.Generic;
	using Lidgren.Network;

	public interface INetworkList<T> : INetworkVariable<INetworkList<T>>, IList<T> where T : class, new()
	{
		void Initialize(INetworkVariableSender sender, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod);
	}

	public class NetworkList<T> : NetworkVariable<INetworkList<T>>, INetworkList<T> where T : class, new()
	{
		protected NetworkSerialization.SerializeObjectDelegate<T> _serialize;
		protected NetworkSerialization.DeserializeObjectDelegate<T> _deserialize;

		private readonly List<T> list;

		public T this[int index]
		{
			get => list[index];
			set => UpdateAt(index, value);
		}

		public int Count => list.Count;

		public bool IsReadOnly => false;

		public NetworkList() : this(32)
		{

		}

		public NetworkList(int capacity)
		{
			list = new List<T>(capacity);
		}

		public void Initialize(INetworkVariableSender sender, int objectId, NetworkSerialization.SerializeObjectDelegate<T> serializeMethod, NetworkSerialization.DeserializeObjectDelegate<T> deserializeMethod)
		{
			Initialize(sender, objectId);

			_serialize = serializeMethod;
			_deserialize = deserializeMethod;
		}

		public override void Read(NetIncomingMessage reader)
		{
			ListOperation op = (ListOperation)reader.ReadByte();

			if (op == ListOperation.Add)
				list.Add(ReadOneObject(new T(), reader));
			else if (op == ListOperation.Clear)
				list.Clear();
			else if (op == ListOperation.Insert)
			{
				int i = reader.ReadInt32();
				list.Insert(i, ReadOneObject(new T(), reader));
			}
			else if (op == ListOperation.RemoveAt)
				list.RemoveAt(reader.ReadInt32());
			else if (op == ListOperation.UpdateAt)
			{
				int i = reader.ReadInt32();
				list[i] = ReadOneObject(list[i], reader);
			}
			else if (op == ListOperation.Set)
			{
				list.Clear();
				int count = reader.ReadInt32();
				for (int x = 0; x < count; x++)
					list.Add(ReadOneObject(new T(), reader));
			}

			RaiseOnChanged();
		}

		public override void Write(NetOutgoingMessage writer)
		{
			writer.Write((byte)ListOperation.Set);

			writer.Write(list.Count);
			for (int x = 0; x < list.Count; x++)
			{
				WriteOneObject(list[x], writer);
			}
		}

		public void Add(T item)
		{
			list.Add(item);
			SendOperation(ListOperation.Add, item);
		}

		public void Clear()
		{
			list.Clear();
			SendOperation(ListOperation.Clear);
		}

		public bool Contains(T item)
		{
			return list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new System.NotSupportedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return list.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			list.Insert(index, item);

			SendOperation(ListOperation.Insert, index, item);
		}

		public bool Remove(T item)
		{
			int index = list.IndexOf(item);
			if (index != -1)
			{
				RemoveAt(index);
				return true;
			}
			return false;
		}

		public void RemoveAt(int index)
		{
			SendOperation(ListOperation.RemoveAt, index);
			list.RemoveAt(index);
		}

		public void UpdateAt(int index, T obj)
		{
			list[index] = obj;
			SendOperation(ListOperation.UpdateAt, index, obj);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this).GetEnumerator();
		}

		public static NetworkList<T> FromObjectID(int objectId)
		{
			return new NetworkList<T>() { objectId = objectId };
		}

		#region Operations
		enum ListOperation : byte
		{
			Set,        // T[]
			Add,        // T
			Insert,     // int, T
			RemoveAt,   // int
			Clear,      // -
			UpdateAt    // int, T
		}

		private T ReadOneObject(T templateObj, NetIncomingMessage reader)
		{
			if (reader.ReadByte() == 0)
				return default(T);
			_deserialize(templateObj, reader);
			return templateObj;
		}

		private void WriteOneObject(T obj, NetOutgoingMessage writer)
		{
			if (obj == null)
			{
				writer.Write((byte)0);
				return;
			}
			writer.Write((byte)1);
			_serialize(obj, writer);
		}

		private void SendOperation(ListOperation listOp, int index, T obj)
		{
			NetOutgoingMessage writer = sender.CreateWriter(objectId);
			writer.Write((byte)listOp);
			writer.Write(index);
			WriteOneObject(obj, writer);
			sender.SendNetworkVariableWriter(writer);
		}

		private void SendOperation(ListOperation listOp, T obj)
		{
			NetOutgoingMessage writer = sender.CreateWriter(objectId);
			writer.Write((byte)listOp);
			WriteOneObject(obj, writer);
			sender.SendNetworkVariableWriter(writer);
		}

		private void SendOperation(ListOperation listOp, int index)
		{
			NetOutgoingMessage writer = sender.CreateWriter(objectId);
			writer.Write((byte)listOp);
			writer.Write(index);
			sender.SendNetworkVariableWriter(writer);
		}

		private void SendOperation(ListOperation listOp)
		{
			NetOutgoingMessage writer = sender.CreateWriter(objectId);
			writer.Write((byte)listOp);
			sender.SendNetworkVariableWriter(writer);
		}
		#endregion
	}
}
