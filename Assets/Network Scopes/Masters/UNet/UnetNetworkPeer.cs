
#if UNITY_5_3_OR_NEWER
namespace NetworkScopes
{
	using UnityEngine.Networking;

	public abstract class UnetNetworkPeer : NetworkPeer
	{
		public NetworkConnection connection { get; private set; }

		public override bool isConnected { get { return connection.isConnected; } }

		public void Initialize (NetworkConnection conn)
		{
			connection = conn;
		}

		public sealed override IMessageWriter CreateWriter (short msgType)
		{
			return new UnetMessageWriter (msgType);
		}

		public sealed override void Send (IMessageWriter writer)
		{
			// make sure to seal the message before sending it out
			UnetMessageWriter unetWriter = (UnetMessageWriter)writer;
			unetWriter.writer.FinishMessage ();

			byte[] data = unetWriter.writer.AsArray ();

			byte error;
			NetworkTransport.Send (connection.hostId, connection.connectionId, 0, data, unetWriter.writer.Position, out error);
		}

		public sealed override void Disconnect ()
		{
			connection.Disconnect ();
		}

		public sealed override void Dispose ()
		{
			connection.Dispose ();
		}
	}
}
#endif