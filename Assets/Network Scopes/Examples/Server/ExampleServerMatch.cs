
using NetworkScopes;
using UnityEngine;

[Scope(typeof(ExampleClientMatch))]
public partial class ExampleServerMatch : ServerScope<ExamplePeer>
{
	protected override void OnPeerEnteredScope (ExamplePeer peer)
	{
		// notify everyone that this peer has joined the game
		SendToPeers(Peers).PlayerJoined(peer.UserName);
	}

	[Signal]
	public void Play()
	{
		// send the Signal notifying everyone of this play
		SendToPeers(Peers).PlayerPlayed(SenderPeer.UserName);
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
