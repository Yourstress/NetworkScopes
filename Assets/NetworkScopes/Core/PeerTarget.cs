using System.Collections.Generic;

namespace NetworkScopes
{
	public class PeerTarget
	{
		private INetworkPeer _targetPeer;
		private IEnumerable<INetworkPeer> _targetGroup;

		public bool isMultipleTargets
		{
			get { return _targetGroup != null; }
		}

		public INetworkPeer TargetPeer
		{
			get { return _targetPeer; }
			set
			{
				_targetPeer = value;
				_targetGroup = null;
				TargetPeerGroupException = null;
			}
		}

		public IEnumerable<INetworkPeer> TargetPeerGroup
		{
			get { return _targetGroup; }
			set
			{
				_targetPeer = null;
				_targetGroup = value;
				TargetPeerGroupException = null;
			}
		}

		public INetworkPeer TargetPeerGroupException;
	}
}