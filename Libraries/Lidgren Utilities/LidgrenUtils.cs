using Lidgren.Network;

namespace NetworkScopes.Utilities
{
	public class LidgrenUtils
	{
		public static NetPeerConfiguration CreateServerConfiguration(string appName, int listenPort)
		{
			NetPeerConfiguration config = CreateSharedConfiguration(appName);
			config.Port = listenPort;
			config.MaximumConnections = 2048;
			return config;
		}
		
		public static NetPeerConfiguration CreateClientConfiguration(string appName)
		{
			NetPeerConfiguration config = CreateSharedConfiguration(appName);
			return config;
		}

		public static NetPeerConfiguration CreateSharedConfiguration(string appName)
		{
			NetPeerConfiguration config = new NetPeerConfiguration(appName)
			{
				PingInterval = 4,
				ConnectionTimeout = 15,
				MaximumHandshakeAttempts = 100,
			};
			
			// disable all unused message types to avoid unnecessary GC allocs
			config.DisableMessageType(NetIncomingMessageType.Receipt);
			config.DisableMessageType(NetIncomingMessageType.DiscoveryRequest);
			config.DisableMessageType(NetIncomingMessageType.DiscoveryResponse);
			config.DisableMessageType(NetIncomingMessageType.DebugMessage);
			config.DisableMessageType(NetIncomingMessageType.WarningMessage);
			config.DisableMessageType(NetIncomingMessageType.ErrorMessage);
			
			return config;
		}
	}
}