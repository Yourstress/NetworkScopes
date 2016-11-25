
namespace NetworkScopes
{
	using System.Collections.Generic;

	public class BaseServerScope
	{
		public void Initialize()
		{
		}
	}

	public class ServerScope<TPeer> : BaseServerScope where TPeer : IPeer, new()
	{
		private Dictionary<object,TPeer> connectionPeers;
	}
}