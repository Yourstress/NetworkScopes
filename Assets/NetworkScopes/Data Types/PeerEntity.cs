
namespace NetworkScopes
{
	using UnityEngine;

	public class PeerEntity
	{
		public float connectTime { get; private set; }
		public float connectDuration { get { return Time.time - connectTime; } }

		public PeerEntity()
		{
			connectTime = Time.time;
		}
	}
}