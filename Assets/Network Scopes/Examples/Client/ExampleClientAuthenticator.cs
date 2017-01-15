
using NetworkScopes;

[Scope(typeof(ExampleServerAuthenticator))]
public partial class ExampleClientAuthenticator : ClientScope
{
	protected override void OnEnterScope ()
	{
		SendToServer.Authenticate("sour", "testpw");
	}
}
