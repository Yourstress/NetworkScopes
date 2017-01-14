
namespace NetworkScopes.UNet
{
	public abstract class NetworkFactory
	{
		public abstract INetworkWriter CreateRawWriter(short msgType);

		public INetworkWriter CreateAuthenticateMessage(int targetScopeID)
		{
			INetworkWriter writer = CreateRawWriter(UnetUtil.ValidateMsgType(NetworkMsgType.Authenticate));
			writer.WriteInt32(targetScopeID);
			return writer;
		}

		public INetworkWriter CreateSignalMessage(short scopeChannelID, int signalType)
		{
			INetworkWriter writer = CreateRawWriter(UnetUtil.ValidateMsgType(NetworkMsgType.ScopeSignal));
			writer.WriteInt16(scopeChannelID);
			writer.WriteInt32(signalType);
			return writer;
		}
	}
}