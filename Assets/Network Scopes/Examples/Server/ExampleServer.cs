
using NetworkScopes;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ExampleServer : MasterServer<ExamplePeer>
{
	public static ExampleServerLobby Lobby { get; private set; }

	public ExampleServer()
	{
		// register a new authentication scope and set it as the default
		RegisterScope<ExampleServerAuthenticator>(0, true);

		// register a new server scope to which authenticated users will be redirected
		Lobby = RegisterScope<ExampleServerLobby>(1, false);
	}

	#region implemented abstract members of MasterServerScope
	protected override ExamplePeer CreatePeer (NetworkConnection connection)
	{
		return new ExamplePeer(connection);
	}

	protected override void DestroyPeer (ExamplePeer peer)
	{
		// nothing to clean-up
	}
	#endregion
}