
namespace NetworkScopes
{
	using System;

	public abstract class BaseClientProvider : IClientProvider, IClientCallbackHandler
	{
		private string lastHost;
		private int lastPort;

		public bool isConnected { get; private set; }
		public bool isConnecting { get; private set; }

		public event Action OnConnected = delegate {};
		public event Action OnDisconnected = delegate {};

		public bool enableLogging;

		protected IClientCallbackHandler callbackHandler { get; private set; }

		protected abstract void ConnectClient(string hostname, int port);
		protected abstract void DisconnectClient();

		public void Connect(string serverHostname, int serverPort)
		{
			if (isConnected)
				throw new Exception("Client is already connected to a server.");

			if (isConnecting)
				throw new Exception("Client is already attempting to connect to a server.");

			// set last hostname and port in order to be able to connect client
			lastHost = serverHostname;
			lastPort = serverPort;

			if (enableLogging)
				ScopeUtils.Log("[Client - {0}] Connecting to {1}:{2}.", GetType().Name, serverHostname, serverPort);

			ConnectClient(lastHost, lastPort);

			isConnecting = true;
		}

		public void Disconnect ()
		{
			if (!isConnected)
				throw new Exception("Client is not connected.");

			isConnected = false;
			isConnecting = false;

			DisconnectClient();
		}

		#region IClientCallbacks implementation
		public void OnConnect ()
		{
			OnConnected();
		}

		public void OnDisconnect ()
		{
			OnDisconnect();
		}

		public void OnReceiveRaw (INetworkReader reader)
		{
			UnityEngine.Debug.Log("Received something that starts with " + reader.ReadChar());
		}
		#endregion
	}
}