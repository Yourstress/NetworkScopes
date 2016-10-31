
#if UNITY_5_3_OR_NEWER
namespace NetworkScopes
{
	using UnityEngine.Networking;

	public class UnetMessageWriter : IMessageWriter
	{
		public NetworkWriter writer { get; private set; }

		public UnetMessageWriter ()
		{
			writer = new NetworkWriter ();
		}

		public UnetMessageWriter (short msgType)
		{
			writer = new NetworkWriter ();
			writer.StartMessage (msgType);
		}

		public void Write (string value)
		{
			writer.Write (value);
		}

		public void Write (bool value)
		{
			writer.Write (value);
		}

		public void Write (int value)
		{
			writer.Write (value);
		}

		public void Write (short value)
		{
			writer.Write (value);
		}

		public void Write (byte value)
		{
			writer.Write (value);
		}

		public void Write (float value)
		{
			writer.Write (value);
		}
	}
}
#endif