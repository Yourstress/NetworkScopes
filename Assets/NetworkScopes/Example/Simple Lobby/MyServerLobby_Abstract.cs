using NetworkScopes;

[Generated]
public abstract class MyServerLobby_Abstract : ServerScope<MyServerLobby_Abstract.ISender>, MyServerLobby_Abstract.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		void FoundMatch(LobbyMatch match);
	}

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

	protected abstract int GetOnlinePlayerCount();
	protected abstract void LookForMatch();
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
		LookForMatch();
	}

}
