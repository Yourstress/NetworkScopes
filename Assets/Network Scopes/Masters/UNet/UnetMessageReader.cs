
#if UNITY_5_3_OR_NEWER
namespace NetworkScopesV2
{
	using UnityEngine.Networking;

	public class UnetMessageReader : IMessageReader
	{
		public NetworkReader reader { get; private set; }

		public UnetMessageReader (byte[] buffer)
		{
			this.reader = new NetworkReader (buffer);
		}

		public UnetMessageReader (NetworkReader reader)
		{
			this.reader = reader;
		}

		public string ReadString ()
		{
			return reader.ReadString ();
		}

		public bool ReadBoolean ()
		{
			return reader.ReadBoolean ();
		}

		public int ReadInt32 ()
		{
			return reader.ReadInt32 ();
		}

		public short ReadInt16 ()
		{
			return reader.ReadInt16 ();
		}

		public byte ReadByte ()
		{
			return reader.ReadByte ();
		}

		public float ReadSingle ()
		{
			return reader.ReadSingle ();
		}
	}
}
#endif