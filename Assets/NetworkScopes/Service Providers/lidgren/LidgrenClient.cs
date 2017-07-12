using System;
using System.Runtime.Remoting.Messaging;
using Lidgren.Network;
using UnityEngine;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public class LidgrenClient : NetworkClient, IDisposable
	{
		public override bool IsConnecting
		{
			get
			{
				return netClient != null &&
						netClient.ConnectionStatus >= NetConnectionStatus.InitiatedConnect &&
				        netClient.ConnectionStatus <= NetConnectionStatus.RespondedConnect;
			}
		}

		public override bool IsConnected
		{
			get { return netClient != null && netClient.ConnectionStatus == NetConnectionStatus.Connected; }
		}

		public override event Action OnConnected = delegate { };
		public override event Action OnDisconnected = delegate { };

		public NetClient netClient { get; private set; }

		private LidgrenMessageReceiver _receiver;

		/// <summary>
		/// Override to customize client configuration.
		/// </summary>
		/// <returns></returns>
		protected virtual NetPeerConfiguration CreateNetPeerConfiguration()
		{
			return LidgrenUtilities.CreateDefaultConfiguration();
		}

		public void Initialize()
		{
			netClient = new NetClient(CreateNetPeerConfiguration());
			netClient.Start();

			_receiver = new LidgrenMessageReceiver(netClient, OnReceiveMessage);
		}

		public override void Connect(string hostnameOrIP, int port)
		{
			if (netClient == null)
			{
				Initialize();
			}

			netClient.Connect(hostnameOrIP, port);
		}

		public override void Disconnect()
		{
			netClient.Disconnect(null);
		}

		// handle message received by the library
		private void OnReceiveMessage(NetIncomingMessage msg)
		{
			switch (msg.MessageType)
			{
				case NetIncomingMessageType.StatusChanged:
				{
					NetConnectionStatus status = (NetConnectionStatus) msg.ReadByte();

					if (status == NetConnectionStatus.Connected)
					{
						OnConnected();
					}
					else if (status == NetConnectionStatus.Disconnected)
					{
						WillDisconnect();
						OnDisconnected();
					}

					break;
				}
				case NetIncomingMessageType.Data:
					ProcessSignal(new LidgrenSignalReader(msg));
					break;
				default:
					LidgrenUtilities.ParseMessage("[Client] ", msg);
					break;
			}
		}

		public void Dispose()
		{
			if (_receiver != null)
				_receiver.Dispose();
		}

		public override ISignalWriter CreateSignal(short scopeChannel)
		{
			return new LidgrenSignalWriter(netClient.CreateMessage(), scopeChannel);
		}

		public override void SendSignal(ISignalWriter signal)
		{
			netClient.ServerConnection.SendMessage(((LidgrenSignalWriter) signal).netMessage, NetDeliveryMethod.ReliableOrdered, 0);
		}
	}
}