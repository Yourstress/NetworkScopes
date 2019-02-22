
namespace NetworkScopes
{
	using Lidgren.Network;
	using System;

	public interface INetworkVariable
	{
		int objectId { get; }
		object RawValue { get; }

		void Read(NetIncomingMessage reader);
		void Write(NetOutgoingMessage writer);
	}

	public interface INetworkVariable<T> : INetworkVariable
	{
		T Value { get; }

		event Action<T> OnChanged;

		void Bind(Action<T> bindMethod);
		void Unbind(Action<T> bindMethod);
	}

	public interface INetworkVariableSender
	{
		NetOutgoingMessage CreateWriter(int objectId);
		void SendNetworkVariableWriter(NetOutgoingMessage msg);
	}

	public abstract class NetworkVariable<T> : INetworkVariable<T>
	{
		protected T _value;
		public T Value
		{
			get => _value;
			set
			{
				_value = value;
				SendVariableUpdate();
			}
		}

		public object RawValue => _value;

		public event Action<T> OnChanged;

		protected INetworkVariableSender sender;
		public int objectId { get; protected set; }

		protected void Initialize(INetworkVariableSender msgSender, int objectId)
		{
			sender = msgSender;
			this.objectId = objectId;
		}

		public abstract void Read(NetIncomingMessage reader);
		public abstract void Write(NetOutgoingMessage writer);

		private void SendVariableUpdate()
		{
			NetOutgoingMessage writer = sender.CreateWriter(objectId);

			Write(writer);

			sender.SendNetworkVariableWriter(writer);
		}

		protected void RaiseOnChanged()
		{
			OnChanged?.Invoke(_value);
		}

		public void Bind(Action<T> bindMethod)
		{
			OnChanged += bindMethod;

			if (_value != null)
				RaiseOnChanged();
		}

		public void Unbind(Action<T> bindMethod)
		{
			OnChanged -= bindMethod;
		}
	}
}
