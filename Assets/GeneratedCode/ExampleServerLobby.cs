using System.Collections.Generic;
using NetworkScopes;
using UnityEngine.Networking;
using System;

public partial class ExampleServerLobby
{
	private RemoteExampleClientLobby _Remote;
	public void Receive_JoinMatch(NetworkReader reader)
	{
		String matchName = reader.ReadString();
		JoinMatch(matchName);
	}
	
	public void Receive_CreateMatch(NetworkReader reader)
	{
		String matchName = reader.ReadString();
		CreateMatch(matchName);
	}
	
	public RemoteExampleClientLobby SendToPeer(ExamplePeer targetPeer)
	{
		if (_Remote == null)
			_Remote = new RemoteExampleClientLobby(this);
		TargetPeer = targetPeer;
		return _Remote;
	}
	
	public RemoteExampleClientLobby ReplyToPeer()
	{
		if (_Remote == null)
			_Remote = new RemoteExampleClientLobby(this);
		TargetPeer = SenderPeer;
		return _Remote;
	}
	
	public RemoteExampleClientLobby SendToPeers(IEnumerable<ExamplePeer> targetPeerGroup)
	{
		if (_Remote == null)
			_Remote = new RemoteExampleClientLobby(this);
		TargetPeerGroup = targetPeerGroup;
		return _Remote;
	}
	
	public class RemoteExampleClientLobby
	{
		private INetworkSender _netSender;
		public RemoteExampleClientLobby(INetworkSender netSender)
		{
			_netSender = netSender;
		}
		
		public void OnMatchList(String[] matches)
		{
			NetworkWriter writer = _netSender.CreateWriter(-2044102460);
			writer.Write(matches.Length);
			for (int _arrCounter = 0; _arrCounter < matches.Length; _arrCounter++)
			writer.Write(matches[_arrCounter]);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void OnMatchNotFound(String matchName)
		{
			NetworkWriter writer = _netSender.CreateWriter(-67105387);
			writer.Write(matchName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
		public void OnMatchAlreadyExists(String matchName)
		{
			NetworkWriter writer = _netSender.CreateWriter(-21368466);
			writer.Write(matchName);
			_netSender.PrepareAndSendWriter(writer);
		}
		
	}
}
