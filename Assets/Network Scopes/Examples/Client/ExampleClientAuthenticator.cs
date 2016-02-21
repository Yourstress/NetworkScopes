
using NetworkScopes;

public class ExampleClientAuthenticator : ClientScope<ExamplePeer,ExampleServerAuthenticator>
{
	protected override void OnEnterScope ()
	{
		Server.Authenticate("sour", "testpw");
	}
}
