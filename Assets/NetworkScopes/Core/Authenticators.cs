
namespace NetworkScopes
{
	public interface IServerAuthenticator
	{
		BaseServerScope targetScope { get; set; }
	}
	
	public abstract class BaseServerAuthenticator : IServerAuthenticator
	{
		public BaseServerScope targetScope { get; set; }

		void AcceptPeerEntity()
		{
//			targetScope.Handover();
		}

		void RejectPeerEntity()
		{
			// send msg
//			UnityEngine.Debug.Log("Reject");
		}
	}

	public abstract class BaseClientAuthenticator : IClientAuthenticator
	{
		protected IClientProvider client { get; private set; }

		public void Initialize(IClientProvider client)
		{
			this.client = client;
		}

		public void Authenticate(INetworkWriter writer)
		{
//			client.SendRaw(writer);
		}
	}

	public interface IClientAuthenticator
	{
		void Authenticate(INetworkWriter writer);
	}
}
