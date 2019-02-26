namespace NetworkScopes
{
	public interface ISerializable
	{
		void Read(IncomingNetworkPacket packet);
		void Write(OutgoingNetworkPacket packet);
	}
}