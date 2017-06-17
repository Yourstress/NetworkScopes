using NetworkScopes;

[Generated]
public class MyClientLobby : ClientScope<MyClientLobby.ISender>, MyClientLobby.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		void GetOnlinePlayerCount();
		void LookForMatch();
	}

	public delegate void FoundMatchDelegate(LobbyMatch match);

	public event FoundMatchDelegate OnFoundMatch = delegate {};

	protected override ISender GetScopeSender()
	{
		return this;
	}

	void ISender.GetOnlinePlayerCount()
	{
		ISignalWriter writer = CreateSignal(-632556091 /*hash 'GetOnlinePlayerCount'*/);
		SendSignal(writer);
	}

	void ISender.LookForMatch()
	{
		ISignalWriter writer = CreateSignal(-625843621 /*hash 'LookForMatch'*/);
		SendSignal(writer);
	}

	protected virtual void FoundMatch(LobbyMatch match)
	{
	}

	protected void Receive_FoundMatch(ISignalReader reader)
	{
		LobbyMatch match = new LobbyMatch();;
		match.Deserialize(reader);
		OnFoundMatch(match);
		FoundMatch(match);
	}

}
