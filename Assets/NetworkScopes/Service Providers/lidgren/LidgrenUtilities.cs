using System.Reflection;
using Lidgren.Network;
using UnityEngine;

namespace NetworkScopes.ServiceProviders.Lidgren
{
	public static class LidgrenUtilities
	{
		public static NetPeerConfiguration CreateDefaultConfiguration()
		{
			return new NetPeerConfiguration("Default");
		}

		public static void ParseMessage(string prefix, NetIncomingMessage msg)
		{
			switch (msg.MessageType)
			{
				case NetIncomingMessageType.VerboseDebugMessage:
				case NetIncomingMessageType.DebugMessage:
				case NetIncomingMessageType.WarningMessage:
				case NetIncomingMessageType.ErrorMessage:
					Debug.Log(prefix + "Received " + msg.ReadString());
					break;
				//				case NetIncomingMessageType.Error:
				//					break;
				case NetIncomingMessageType.StatusChanged:
					Debug.Log(prefix + "StatusChanged: " + (NetConnectionStatus) msg.ReadByte());
					break;
				//				case NetIncomingMessageType.UnconnectedData:
				//					break;
				//				case NetIncomingMessageType.ConnectionApproval:
				//					break;
				//				case NetIncomingMessageType.Data:
				//					break;
				//				case NetIncomingMessageType.Receipt:
				//					break;
				//				case NetIncomingMessageType.DiscoveryRequest:
				//					break;
				//				case NetIncomingMessageType.DiscoveryResponse:
				//					break;
				//
				//				case NetIncomingMessageType.NatIntroductionSuccess:
				//					break;
				//				case NetIncomingMessageType.ConnectionLatencyUpdated:
				//					break;
					break;
				default:
					Debug.Log(prefix + "Unhandled message of type " + msg.MessageType);
					break;
			}
		}
	}
}