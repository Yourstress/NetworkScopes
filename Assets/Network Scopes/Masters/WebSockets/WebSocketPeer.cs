
namespace NetworkScopes
{
	public class WebSocketPeer : NetworkPeer
	{
		public WebSocketConnectionHandler connection { get; private set; }

		public void Initialize (WebSocketConnectionHandler connection)
		{
			this.connection = connection;
		}

		#region implemented abstract members of NetworkPeer
		public override IMessageWriter CreateWriter (short msgType)
		{
			return new SocketMessageWriter(msgType, false);
		}
		public override void Send (IMessageWriter writer)
		{
			SocketMessageWriter sw = (SocketMessageWriter)writer;
			sw.FinishMessage();

			// make sure to seal the message before sending it out
			byte[] buffer = sw.stream.ToArray();
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (byte b in buffer)
				sb.Append(b.ToString());

			ScopeUtils.Log(sb.ToString());
			connection.Context.WebSocket.Send(buffer);

			sw.Dispose();
		}
		public override void Disconnect ()
		{
			connection.Context.WebSocket.CloseAsync();
		}
		public override void Dispose ()
		{
			connection = null;
		}
		public override bool isConnected {
			get {
				return connection.State == WebSocketSharp.WebSocketState.Open;
			}
		}
		public override string ipAddress {
			get {
				return connection.Context.Host;
			}
		}
		#endregion
	}
}