using Lidgren.Network;

namespace NetworkScopes
{
	// Contains an instance of T1 or T2
	[NetworkSerialization(NetworkSerializeSettings.Custom)]
	public struct Maybe<TValue,TError>
		where TValue : class
		where TError : class
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

		public static implicit operator Maybe<TValue,TError>(TValue value)
		{
			return new Maybe<TValue, TError> { Value = value };
		}

		public static implicit operator Maybe<TValue,TError>(TError error)
		{
			return new Maybe<TValue, TError> { Error = error };
		}

		public static void NetworkSerialize(Maybe<TValue, TError> value, NetOutgoingMessage writer)
		{
			writer.Write(value.HasValue);

			if (value.HasValue)
				NetworkSerializer.Write(value._value, writer);
			else
				NetworkSerializer.Write(value._error, writer);
		}

		public static void NetworkDeserialize(Maybe<TValue, TError> value, NetIncomingMessage reader)
		{
			value.HasValue = reader.ReadBoolean();

			if (value.HasValue)
				NetworkSerializer.Read(value._value, reader);
			else
				NetworkSerializer.Read(value._error, reader);
		}
	}
}