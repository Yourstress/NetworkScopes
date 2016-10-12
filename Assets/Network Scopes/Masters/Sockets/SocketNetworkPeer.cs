
namespace NetworkScopesV2
{
	using System.Net.Sockets;
	using System.Net;

	public abstract class SocketNetworkPeer : NetworkPeer
	{
		public TcpClient connection { get; private set; }

		public override bool isConnected { get { return connection.Connected; } }
		public override string ipAddress
		{
			get
			{
				byte[] address = ((IPEndPoint)connection.Client.RemoteEndPoint).Address.GetAddressBytes();
				return new IPAddress(address).ToString();
			}
		}

		public void Initialize (TcpClient client)
		{
			connection = client;
		}

		public sealed override IMessageWriter CreateWriter (short msgType)
		{
			return new SocketMessageWriter (msgType);
		}

		public sealed override void Send (IMessageWriter writer)
		{
			SocketMessageWriter sw = (SocketMessageWriter)writer;
			sw.FinishMessage();


			// make sure to seal the message before sending it out
			byte[] buffer = sw.stream.ToArray();

			var stream = connection.GetStream();
			stream.Write(buffer, 0, buffer.Length);
			stream.Flush();

			sw.Dispose();
		}

		public sealed override void Disconnect ()
		{
			#if UNITY_5_3_OR_NEWER
			connection.Close();
			#else
			connection.Dispose();
			#endif
		}

		public sealed override void Dispose ()
		{
		}
	}
}