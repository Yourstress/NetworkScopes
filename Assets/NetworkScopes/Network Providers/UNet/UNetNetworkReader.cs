
namespace NetworkScopes.UNet
{
	using UnityEngine.Networking;

	public class UNetNetworkReader : INetworkReader
	{
		private NetworkReader reader;

		public UNetNetworkReader ()
		{
			reader = new NetworkReader ();
		}

		public UNetNetworkReader(NetworkReader existingReader)
		{
			reader = existingReader;
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

		public char ReadChar ()
		{
			return reader.ReadChar ();
		}
	}
}