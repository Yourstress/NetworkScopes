
namespace NetworkScopes.UNet
{
	public sealed class UNetNetworkFactory : NetworkFactory
	{
		public sealed override INetworkWriter CreateRawMessage (short msgType)
		{
			return new UNetNetworkWriter (msgType);
		}
	}
}