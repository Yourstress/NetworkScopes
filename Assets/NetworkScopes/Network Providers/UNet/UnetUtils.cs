
namespace NetworkScopes.UNet
{
	using UnityEngine.Networking;
	using UnityEngine.Assertions;

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

		public static short ValidateMsgType(short msgType)
		{
			// modify msgtype to go up an offset of 50
			msgType += 50;

			// make sure we're not using Unity-defined msg types
			Assert.IsTrue(msgType >= 50);

			return msgType;
		}
	}
}