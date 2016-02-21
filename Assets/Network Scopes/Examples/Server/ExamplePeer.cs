using UnityEngine;
using System.Collections;
using NetworkScopes;
using UnityEngine.Networking;

public class ExamplePeer : IScopePeer
{
	#region IScopePeer implementation
	public NetworkConnection connection { get; private set; }
	public float connectTime { get; private set; }
	#endregion

	public string userName { get; private set; }
	
	public ExamplePeer(NetworkConnection conn)
	{
		connection = conn;
	}

	public void SetAuthenticated(string peerUserName)
	{
		userName = peerUserName;
	}
}
