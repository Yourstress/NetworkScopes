using System.Collections.Generic;
using NetworkScopes;
using UnityEngine.Networking;
using System;

public partial class ExampleClientAuthenticator
{
	private RemoteExampleServerAuthenticator _Remote;
	public RemoteExampleServerAuthenticator SendToServer
	{
		get
		{
			if (_Remote == null)
				_Remote = new RemoteExampleServerAuthenticator(this);
			return _Remote;
		}
	}
	public class RemoteExampleServerAuthenticator
	{
		private INetworkSender _netSender;
		public RemoteExampleServerAuthenticator(INetworkSender netSender)
		{
			_netSender = netSender;
		}
		
		public void Authenticate(String userName, String passwordHash)
		{
			NetworkWriter writer = _netSender.CreateWriter(1885436661);
			writer.Write(userName);
			writer.Write(passwordHash);
			_netSender.PrepareAndSendWriter(writer);
		}
		
	}
}
