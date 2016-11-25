
namespace NetworkScopes
{
	public class NetworkEither<TSerializable1,TSerializable2> : INetworkSerializable where TSerializable1 : INetworkSerializable, new() where TSerializable2 : INetworkSerializable, new()
	{
		public bool isFirstArgument { get; private set; }

		public TSerializable1 firstArgument;
		public TSerializable2 secondArgument;

		public NetworkEither (TSerializable1 firstArg)
		{
			isFirstArgument = true;
			firstArgument = firstArg;
		}

		public NetworkEither (TSerializable2 secondArg)
		{
			isFirstArgument = false;
			secondArgument = secondArg;
		}

		#region INetworkSerializable implementation

		public void NetworkSerialize (INetworkWriter writer)
		{
			writer.WriteBoolean (isFirstArgument);
	
			if (isFirstArgument)
				firstArgument.NetworkSerialize (writer);
			else
				secondArgument.NetworkSerialize (writer);
		}

		public void NetworkDeserialize (INetworkReader reader)
		{
			isFirstArgument = reader.ReadBoolean ();
	
			firstArgument = isFirstArgument ? NetworkSerializer.DeserializeObject<TSerializable1> (reader) : default(TSerializable1);
			secondArgument = !isFirstArgument ? NetworkSerializer.DeserializeObject<TSerializable2> (reader) : default(TSerializable2);
		}

		#endregion
	}
}