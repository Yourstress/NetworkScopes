using NetworkScopes;

[Generated]
public class MyServerLobby : ServerScope<MyServerLobby.ISender>, MyServerLobby.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		void FoundMatch(LobbyMatch match);
		void FoundMatches(LobbyMatch[] matches);
	}

	public delegate void LookForMatchDelegate();

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

	void ISender.FoundMatches(LobbyMatch[] matches)
	{
		ISignalWriter writer = CreateSignal(771868529 /*hash 'FoundMatches'*/);
		writer.WriteInt32(matches.Length);
		for (int matches_x = 0; matches_x < matches.Length; matches_x++)
		{
			matches[matches_x].Serialize(writer);
		}
		SendSignal(writer);
	}

	protected virtual int GetOnlinePlayerCount()
	{
		return default(int);
	}

	protected virtual void LookForMatch()
	{
	}

	protected void ReceiveSignal_GetOnlinePlayerCount(ISignalReader reader)
	{
		int promiseID = reader.ReadPromiseID();
		int promiseValue = GetOnlinePlayerCount();
		ISignalWriter writer = CreateSignal(-1825208728 /*hash '#GetOnlinePlayerCount'*/);
		writer.WriteInt32(promiseID);
		writer.WriteInt32(promiseValue);
		SendSignal(writer, SenderPeer);
	}

	protected void ReceiveSignal_LookForMatch(ISignalReader reader)
	{
		OnLookForMatch();
		LookForMatch();
	}

}
