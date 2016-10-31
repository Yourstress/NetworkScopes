
namespace NetworkScopes
{
	using System.IO;
	using System;
	
	public class SocketMessageWriter : IMessageWriter, IDisposable
	{
		private bool writeSize;
		public MemoryStream stream { get; private set; }

		BinaryWriter writer;
		public SocketMessageWriter(short msgType, bool writeSize)
		{
			stream = new MemoryStream();
			writer = new BinaryWriter(stream);

			this.writeSize = writeSize;

			if (writeSize)
				writer.Write((ushort)0); // size
			
			writer.Write(msgType);
		}

		public void FinishMessage()
		{
			if (writeSize)
			{
				#if UNITY_5_3_OR_NEWER
				
				byte[] buffer = stream.GetBuffer();
				byte[] sizeBytes = BitConverter.GetBytes((ushort)(stream.Position - sizeof(ushort)));
				
				for (int x = 0; x < sizeBytes.Length; x++)
				buffer[x] = sizeBytes[x];
				#else
				
				long streamPos = stream.Position;
				ushort msgLength = (ushort)(streamPos - sizeof(ushort));
				stream.Position = 0;
				stream.Write(BitConverter.GetBytes(msgLength), 0, sizeof(ushort));
				stream.Position = streamPos;
				
				#endif
			}
		}

		public void Dispose()
		{
			#if UNITY_5_3_OR_NEWER
			writer.Close();
			stream.Close();
			#else
			writer.Dispose();
			stream.Dispose();
			#endif
		}

		public void Write (string value) { writer.Write(value); }
		public void Write (bool value) { writer.Write(value); }
		public void Write (int value) { writer.Write(value); }
		public void Write (short value) { writer.Write(value); }
		public void Write (byte value) { writer.Write(value); }
		public void Write (float value) { writer.Write(value); }
	}
}