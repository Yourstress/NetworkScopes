using NetworkScopes;

[Generated]
public class MyServerLobby : ServerScope<MyServerLobby.ISender>, MyServerLobby.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		void FoundMatch(LobbyMatch match);
	}

	public delegate void GetOnlinePlayerCountDelegate();
	public delegate void LookForMatchDelegate();

	public event GetOnlinePlayerCountDelegate OnGetOnlinePlayerCount = delegate {};
	public event LookForMatchDelegate OnLookForMatch = delegate {};

	protected override ISender GetScopeSender()
	{
		return this;
	}

	void ISender.FoundMatch(LobbyMatch match)
	{
		ISignalWriter writer = CreateSignal(-106459261 /*hash 'FoundMatch'*/);
		match.Serialize(writer);
		SendSignal(writer);
	}

	protected virtual void GetOnlinePlayerCount()
	{
	}

	protected virtual void LookForMatch()
	{
	}

	protected void Receive_GetOnlinePlayerCount(ISignalReader reader)
	{
		OnGetOnlinePlayerCount();
		GetOnlinePlayerCount();
	}

	protected void Receive_LookForMatch(ISignalReader reader)
	{
		OnLookForMatch();
		LookForMatch();
	}

}
