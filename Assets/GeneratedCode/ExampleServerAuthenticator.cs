using UnityEngine.Networking;
using System;

public partial class ExampleServerAuthenticator
{
	public void Receive_Authenticate(NetworkReader reader)
	{
		String userName = reader.ReadString();
		String passwordHash = reader.ReadString();
		Authenticate(userName, passwordHash);
	}
	
}
