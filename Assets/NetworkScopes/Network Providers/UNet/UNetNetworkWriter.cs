
namespace NetworkScopes.UNet
{
	using UnityEngine.Networking;

	public class UNetNetworkWriter : INetworkWriter
	{
		private NetworkWriter writer;

		public UNetNetworkWriter ()
		{
			writer = new NetworkWriter ();
		}

		public void WriteString (string value)
		{
			writer.Write (value);
		}

		public void WriteBoolean (bool value)
		{
			writer.Write (value);
		}

		public void WriteInt32 (int value)
		{
			writer.Write (value);
		}

		public void WriteInt16 (short value)
		{
			writer.Write (value);
		}

		public void WriteByte (byte value)
		{
			writer.Write (value);
		}

		public void WriteSingle (float value)
		{
			writer.Write (value);
		}

		public void WriteChar (char value)
		{
			writer.Write (value);
		}
	}
}