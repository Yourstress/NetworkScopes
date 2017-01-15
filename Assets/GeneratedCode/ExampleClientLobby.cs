using System.Collections.Generic;
using NetworkScopes;
using UnityEngine.Networking;
using System;

public partial class ExampleClientLobby
{
	private RemoteExampleServerLobby _Remote;
	public RemoteExampleServerLobby SendToServer
	{
		get
		{
			if (_Remote == null)
				_Remote = new RemoteExampleServerLobby(this);
			return _Remote;
		}
	}
	public void Receive_OnMatchList(NetworkReader reader)
	{
		Int32 matches_count = reader.ReadInt32();
		System.String[] matches = new System.String[matches_count];
		for (int matches_index = 0; matches_index < matches_count; matches_index++)
		matches[matches_index] = reader.ReadString();
		OnMatchList(matches);
	}
	
	public void Receive_OnMatchNotFound(NetworkReader reader)
	{
		String matchName = reader.ReadString();
		OnMatchNotFound(matchName);
	}
	
	public void Receive_OnMatchAlreadyExists(NetworkReader reader)
	{
		String matchName = reader.ReadString();
		OnMatchAlreadyExists(matchName);
	}
	
	public class RemoteExampleServerLobby
	{
		private INetworkSender _netSender;
		public RemoteExampleServerLobby(INetworkSender netSender)
		{
			_netSender = netSender;
		}
		
		public void JoinMatch(String matchName)
		{
			NetworkWriter writer = _netSender.CreateWriter(1492702875);
			writer.Write(matchName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void CreateMatch(String matchName)
		{
			NetworkWriter writer = _netSender.CreateWriter(1157115401);
			writer.Write(matchName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
	}
}
