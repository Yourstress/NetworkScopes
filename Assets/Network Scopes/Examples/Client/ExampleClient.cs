
using NetworkScopes;

public class ExampleClient : MasterClient
{
	public ExampleClientLobby Lobby { get; private set; }
	public ExampleClientMatch Match { get; private set; }

	public ExampleClient()
	{
		// register Scopes to receive Signals from the server
		RegisterScope<ExampleClientAuthenticator>(0);
		Lobby = RegisterScope<ExampleClientLobby>(1);
		Match = RegisterScope<ExampleClientMatch>(2);
	}
}
