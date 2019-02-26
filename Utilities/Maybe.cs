
namespace NetworkScopes
{
	// Contains an instance of T1 or T2
	public class Maybe<TValue,TError> : ISerializable
		where TValue : class, new()
		where TError : class, new()
	{
		private TValue _value;
		private TError _error;

		public bool HasValue { get; private set; }

		public TValue Value
		{
			get { return _value; }
			set
			{
				_value = value;
				HasValue = true;
			}
		}

		public TError Error
		{
			get { return _error; }
			set
			{
				_error = value;
				HasValue = false;
			}
		}

		public override string ToString()
		{
			return $"Maybe<{typeof(TValue).Name},{typeof(TError).Name} (Value='{_value}', Error='{_error}')";
		}

		public static implicit operator Maybe<TValue,TError>(TValue value)
		{
			return new Maybe<TValue, TError> { Value = value };
		}

		public static implicit operator Maybe<TValue,TError>(TError error)
		{
			return new Maybe<TValue, TError> { Error = error };
		}

		public void Read(IncomingNetworkPacket packet)
		{
			HasValue = packet.ReadBoolean();

			if (HasValue)
				_value = packet.ReadObject<TValue>();
			else
				_error = packet.ReadObject<TError>();
		}

		public void Write(OutgoingNetworkPacket packet)
		{
			packet.Write(HasValue);

			if (HasValue)
				packet.WriteObject(_value);
			else
				packet.WriteObject(_error);
		}
	}
}