using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using NetworkScopes.Utilities;
using UnityEngine;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public class LidgrenMessageReceiver : IDisposable
	{
		public delegate void ReceiveMessageDelegate(NetIncomingMessage msg);

		private readonly NetPeer _peer;
		private readonly ReceiveMessageDelegate _onReceiveMessage;

		public LidgrenMessageReceiver(NetPeer peer, ReceiveMessageDelegate onReceiveMessage)
		{
			_peer = peer;
			_onReceiveMessage = onReceiveMessage;

			UnityUpdateDispatcher.OnUpdate += Update_ReadMessages;
		}

		public void Dispose()
		{
			UnityUpdateDispatcher.OnUpdate -= Update_ReadMessages;
		}

		// called every frame in Unity
		void Update_ReadMessages()
		{
			NetIncomingMessage msg;

			// read every message until there's no more messages for this frame
			// TODO: optionally configure maximum messages to read per frame
			while ((msg = _peer.ReadMessage()) != null)
			{
				// pass the message over to the subscriber of this object
				_onReceiveMessage(msg);

				// recycle the message to reduce garbage
				_peer.Recycle(msg);
			}
		}
	}
}