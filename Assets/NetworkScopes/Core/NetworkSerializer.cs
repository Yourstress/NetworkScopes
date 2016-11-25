
namespace NetworkScopes
{
	public class NetworkSerializer
	{
		public static TSerializable DeserializeObject<TSerializable>(INetworkReader reader) where TSerializable : INetworkSerializable, new()
		{
			TSerializable obj = new TSerializable();
			obj.NetworkDeserialize(reader);
			return obj;
		}
	}
}