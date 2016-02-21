
using NetworkScopes;
using UnityEngine;

public class ExampleServerMatch : ServerScope<ExamplePeer, ExampleClientMatch>
{
	protected override void OnPeerEnteredScope (ExamplePeer peer)
	{
		// notify everyone that this peer has joined the game
		TargetPeerGroup = Peers.Values;
		Client.PlayerJoined(peer.userName);
	}

	[Signal]
	public void Play()
	{
		// target all the peers before the next Signal call to the client
		TargetPeerGroup = Peers.Values;

		// send the Signal notifying everyone of this play
		Client.PlayerPlayed(SenderPeer.userName);
	}

	[Signal]
	public void TestAutoSerializedObj(ExampleObject obj)
	{
		Debug.Log("Received auto-serialized object: " + obj);
	}

	[Signal]
	public void LeaveMatch ()
	{
		// hand the peer back over to the Lobby Scope - this will trigger the overridable OnPeerExitedScope(ExamplePeer) in order to handle any cleanup
		HandoverPeer(SenderPeer, ExampleServer.Lobby);
	}
}
