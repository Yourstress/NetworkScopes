
using System;

namespace NetworkScopes
{
	public abstract class NetworkPeer : INetworkPeer
	{
		public abstract string ipAddress { get; }

		public bool IsDestroyed { get; private set; }
		
		// this flag is used in order to notify the clients of leaving the Scope they were previously in and avoid discrepancy
		public bool sendExitScopeMsgOnDisconnect { get; private set; }

		public abstract bool IsConnected { get; }
		public abstract void Disconnect();
		
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
				if (IsConnected && closeConnection)
					Disconnect();
			}
		}
		
		/// <summary>
		/// Forces the Peer to disconnect, raising the OnDisconnect event in the process.
		/// This method is also called when a peer disconnects or loses connection normally.
		/// </summary>
		public void DisconnectWithMessage(byte disconnectMsgIdentifier)
		{
			if (IsConnected)
			{
				SignalWriter writer = new SignalWriter();
			
				writer.Write(ScopeChannel.DisconnectMessage);
				writer.Write(disconnectMsgIdentifier);
			
				SendSignal(writer);
			}

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

		protected virtual void Cleanup(bool wasSoftDisconnected)
		{
			
		}

		/// <summary>
		/// Sends instructions to reconnect to the specified host.
		/// </summary>
		/// <param name="hostname">IP Address or Host.</param>
		/// <param name="port">Server port.</param>
		public void Redirect(string hostname, int port)
		{
			SignalWriter writer = new SignalWriter(ScopeChannel.RedirectMessage);
			writer.Write(hostname);
			writer.Write(port);

			SendSignal(writer);

			// trigger early disconnection
			ForceDisconnect(false);
		}

		public abstract void SendSignal(ISignalWriter signal);

		public event Action<INetworkPeer> OnDisconnect = delegate { };
	}
}