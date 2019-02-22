
using Lidgren.Network;

namespace NetworkScopes
{
	using System;

	public abstract class NetworkPeer
	{
		public event Action<NetworkPeer> OnDisconnect = delegate {};

		public bool IsDestroyed { get; private set; }
		public bool sendExitScopeMsgOnDisconnect { get; private set; }
		
		public NetConnection connection { get; private set; }
		
		public bool isConnected
		{
			get { return connection.Status == NetConnectionStatus.Connected; }
		}

		public virtual void Initialize(NetConnection connection)
		{
			this.connection = connection;
		}

		/// <summary>
		/// Forces the Peer to disconnect, raising the OnDisconnect event in the process.
		/// This method is also called when a peer disconnects or loses connection normally.
		/// </summary>
		public void ForceDisconnect(bool closeConnection)
		{
			// manually call raise the Disconnect event if it's set
			if (OnDisconnect != null)
			{
				Cleanup(false);

				IsDestroyed = true;

				OnDisconnect(this);
				OnDisconnect = null;

				// also disconnect if connected and flagged so
				if (closeConnection && isConnected)
					Disconnect();
			}
		}

		public void Disconnect()
		{
			connection.Disconnect(string.Empty);
		}

		protected virtual void Cleanup(bool wasSoftDisconnected)
		{
		}

		/// <summary>
		/// Forces the Peer to disconnect, raising the OnDisconnect event in the process.
		/// This method is also called when a peer disconnects or loses connection normally.
		/// </summary>
		public void DisconnectWithMessage(byte disconnectMsgIdentifier)
		{
			NetOutgoingMessage msg = connection.Peer.CreateMessage();
			
			msg.Write(ScopeMsgType.DisconnectMessage);
			msg.Write(disconnectMsgIdentifier);

			ScopeUtils.SendRawMessage(msg, this);

			// trigger early disconnection
			ForceDisconnect(false);
		}
		
		public void SoftDisconnect()
		{
			if (OnDisconnect != null)
			{
				// this flag is used in order to notify the clients of leaving the Scope they were previously in and avoid discrepancy
				sendExitScopeMsgOnDisconnect = true;

				Cleanup(true);
				
				// raise events to trigger cleanup and removal of peer from the registered scopes
				OnDisconnect(this);
				OnDisconnect = null;
				
				// this peer might be reused later - turn off this flag until this is called again, if it ever is
				sendExitScopeMsgOnDisconnect = false;
			}
		}

		/// <summary>
		/// Sends instructions to reconnect to the specified host.
		/// </summary>
		/// <param name="hostname">IP Address or Host.</param>
		/// <param name="port">Server port.</param>
		public void Redirect(string hostname, int port)
		{
			NetOutgoingMessage msg = connection.Peer.CreateMessage();

			msg.Write(ScopeMsgType.RedirectMessage);
			msg.Write(hostname);
			msg.Write(port);

			ScopeUtils.SendRawMessage(msg, this);

			// trigger early disconnection
			ForceDisconnect(false);
		}
	}
}