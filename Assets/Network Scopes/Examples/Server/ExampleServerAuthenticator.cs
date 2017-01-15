
using System;
using NetworkScopes;

[Scope(typeof(ExampleClientAuthenticator))]
public partial class ExampleServerAuthenticator : ServerScope<ExamplePeer>
{
	[Signal]
	public void Authenticate(string userName, string passwordHash)
	{
		// process the user's credentials - for now we're just checking for null/empty string to keep it simple for this example
		if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(passwordHash))
		{
			// user authenticated successfully - assign player name within this peer's ExamplePeer instance
			SenderPeer.SetAuthenticated(userName);

			// ..then send peer to the Lobby scope
			HandoverPeer(SenderPeer, ExampleServer.Lobby);
		}
		// if they couldn't be authenticated, we might as well disconnect them
		else
		{
			// TODO: we might want to send a disconnection reason rather than just closing the connection
			SenderPeer.Disconnect();
		}
	}
}