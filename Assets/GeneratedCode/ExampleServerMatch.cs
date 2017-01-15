using System.Collections.Generic;
using NetworkScopes;
using UnityEngine.Networking;
using System;

public partial class ExampleServerMatch
{
	private RemoteExampleClientMatch _Remote;
	public void Receive_Play(NetworkReader reader)
	{
		Play();
	}
	
	public void Receive_TestAutoSerializedObj(NetworkReader reader)
	{
		ExampleObject obj = new ExampleObject();
		NetworkSerializer.ExampleObjectSerializer.NetworkDeserialize(obj, reader);
		TestAutoSerializedObj(obj);
	}
	
	public void Receive_LeaveMatch(NetworkReader reader)
	{
		LeaveMatch();
	}
	
	public RemoteExampleClientMatch SendToPeer(ExamplePeer targetPeer)
	{
		if (_Remote == null)
			_Remote = new RemoteExampleClientMatch(this);
		TargetPeer = targetPeer;
		return _Remote;
	}
	
	public RemoteExampleClientMatch ReplyToPeer()
	{
		if (_Remote == null)
			_Remote = new RemoteExampleClientMatch(this);
		TargetPeer = SenderPeer;
		return _Remote;
	}
	
	public RemoteExampleClientMatch SendToPeers(IEnumerable<ExamplePeer> targetPeerGroup)
	{
		if (_Remote == null)
			_Remote = new RemoteExampleClientMatch(this);
		TargetPeerGroup = targetPeerGroup;
		return _Remote;
	}
	
	public class RemoteExampleClientMatch
	{
		private INetworkSender _netSender;
		public RemoteExampleClientMatch(INetworkSender netSender)
		{
			_netSender = netSender;
		}
		
		public void PlayerJoined(String playerName)
		{
			NetworkWriter writer = _netSender.CreateWriter(-350440022);
			writer.Write(playerName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void PlayerLeft(String playerName)
		{
			NetworkWriter writer = _netSender.CreateWriter(-205901144);
			writer.Write(playerName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void PlayerPlayed(String playerName)
		{
			NetworkWriter writer = _netSender.CreateWriter(-181663436);
			writer.Write(playerName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
	}
}
