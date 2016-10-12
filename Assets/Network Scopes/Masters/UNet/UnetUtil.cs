
#if UNITY_5_3_OR_NEWER
namespace NetworkScopesV2
{
	using UnityEngine.Networking;

	public static class UnetUtil
	{
		public static ConnectionConfig CreateConnectionConfig ()
		{
			ConnectionConfig conConfig = new ConnectionConfig ();

			conConfig.NetworkDropThreshold = 20;
			conConfig.DisconnectTimeout = 5000;

			conConfig.AddChannel (QosType.ReliableSequenced);

			return conConfig;
		}
	}
}
#endif