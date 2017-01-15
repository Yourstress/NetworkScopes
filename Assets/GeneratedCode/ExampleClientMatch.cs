using System.Collections.Generic;
using NetworkScopes;
using UnityEngine.Networking;
using System;

public partial class ExampleClientMatch
{
	private RemoteExampleServerMatch _Remote;
	public RemoteExampleServerMatch SendToServer
	{
		get
		{
			if (_Remote == null)
				_Remote = new RemoteExampleServerMatch(this);
			return _Remote;
		}
	}
	public void Receive_PlayerJoined(NetworkReader reader)
	{
		String playerName = reader.ReadString();
		PlayerJoined(playerName);
	}
	
	public void Receive_PlayerLeft(NetworkReader reader)
	{
		String playerName = reader.ReadString();
		PlayerLeft(playerName);
	}
	
	public void Receive_PlayerPlayed(NetworkReader reader)
	{
		String playerName = reader.ReadString();
		PlayerPlayed(playerName);
	}
	
	public class RemoteExampleServerMatch
	{
		private INetworkSender _netSender;
		public RemoteExampleServerMatch(INetworkSender netSender)
		{
			_netSender = netSender;
		}
		
		public void Play()
		{
			NetworkWriter writer = _netSender.CreateWriter(2490196);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void TestAutoSerializedObj(ExampleObject obj)
		{
			NetworkWriter writer = _netSender.CreateWriter(-1696487214);
			NetworkSerializer.ExampleObjectSerializer.NetworkSerialize(obj, writer);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void LeaveMatch()
		{
			NetworkWriter writer = _netSender.CreateWriter(1946358126);
			_netSender.PrepareAndSendWriter(writer);
		}
		
	}
}
