
namespace NetworkScopes
{
	using UnityEngine.Networking;

	public interface IScopePeer
	{
		NetworkConnection connection { get; }
		
		float connectTime { get; }
	}
}
