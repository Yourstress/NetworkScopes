
namespace NetworkScopes
{
	using System;
	using System.Collections.Generic;
	using UnityEngine.Networking;

	public class NetworkPeer : NetworkConnection
	{
		public event Action<NetworkPeer> OnDisconnect = delegate {};

		public bool IsDestroyed { get; private set; }
		public bool sendExitScopeMsgOnDisconnect { get; private set; }

		/// <summary>
		/// Forces the Peer to disconnect, raising the OnDisconnect event in the process.
		/// This method is also called when a peer disconnects or loses connection normally.
		/// </summary>
		public void ForceDisconnect(bool closeConnection)
		{
			// manually call raise the Disconnect event if it's set
			if (OnDisconnect != null)
			{
				OnDisconnect(this);
				OnDisconnect = null;

				IsDestroyed = true;

				// also disconnect if connected and flagged so
				if (closeConnection && isConnected)
					Disconnect();
			}
		}

		/// <summary>
		/// Forces the Peer to disconnect, raising the OnDisconnect event in the process.
		/// This method is also called when a peer disconnects or loses connection normally.
		/// </summary>
		public void DisconnectWithMessage(byte disconnectMsgIdentifier)
		{
			NetworkWriter writer = new NetworkWriter();

			writer.StartMessage(ScopeMsgType.DisconnectMessage);
			writer.Write(disconnectMsgIdentifier);
			writer.FinishMessage();

			ScopeUtils.SendNetworkWriter(writer, this);

			// trigger early disconnection
			ForceDisconnect(false);
		}
		
		public void SoftDisconnect()
		{
			if (OnDisconnect != null)
			{
				// this flag is used in order to notify the clients of leaving the Scope they were previously in and avoid discrepancy
				sendExitScopeMsgOnDisconnect = true;
				
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
			NetworkWriter writer = new NetworkWriter();

			writer.StartMessage(ScopeMsgType.RedirectMessage);
			writer.Write(hostname);
			writer.Write(port);
			writer.FinishMessage();

			ScopeUtils.SendNetworkWriter(writer, this);

			// trigger early disconnection
			ForceDisconnect(false);
		}
	}
}