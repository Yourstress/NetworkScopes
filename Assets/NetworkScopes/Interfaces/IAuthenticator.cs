
namespace NetworkScopes
{
	public abstract class BaseAuthenticator : IAuthenticator
	{
		public BaseServerScope targetScope { get; set; }

		void AcceptPeerEntity()
		{
			UnityEngine.Debug.Log("Accept");
//			targetScope.
		}

		void RejectPeerEntity()
		{
			UnityEngine.Debug.Log("Reject");
		}
	}

	public interface IAuthenticator
	{
		BaseServerScope targetScope { get; set; }
	}
}
