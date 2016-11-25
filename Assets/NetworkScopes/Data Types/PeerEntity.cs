
namespace NetworkScopes
{
	using UnityEngine;

	public class PeerEntity
	{
		public float connectTime { get; private set; }
		public float connectDuration { get { return Time.time - connectTime; } }

		public IServerProvider serviceProvider;

		public PeerEntity(IServerProvider serverProvider)
		{
			connectTime = Time.time;
			this.serviceProvider = serverProvider;
		}
	}
}