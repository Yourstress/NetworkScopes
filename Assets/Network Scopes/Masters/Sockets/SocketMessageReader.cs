
namespace NetworkScopes
{
	using System.IO;

	public class SocketMessageReader : IMessageReader
	{
		public MemoryStream stream { get; private set; }

		BinaryReader reader;

		public SocketMessageReader(byte[] buffer, int bufferLength)
		{
			stream = new MemoryStream(bufferLength);
			stream.Write(buffer, 0, bufferLength);
			stream.Position = 0;

			reader = new BinaryReader(stream);
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

		public byte ReadByte()
		{
			return reader.ReadByte();
		}

		public float ReadSingle()
		{
			return reader.ReadSingle();
		}
	}
}