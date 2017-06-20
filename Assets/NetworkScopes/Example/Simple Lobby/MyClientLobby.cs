using NetworkScopes;

[Generated]
public class MyClientLobby : ClientScope<MyClientLobby.ISender>, MyClientLobby.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		ValuePromise<int> GetOnlinePlayerCount();
		void LookForMatch();
	}

	public delegate void FoundMatchDelegate(LobbyMatch match);
	public delegate void FoundMatchesDelegate(LobbyMatch[] matches);

	public event FoundMatchDelegate OnFoundMatch = delegate {};
	public event FoundMatchesDelegate OnFoundMatches = delegate {};

	protected override ISender GetScopeSender()
	{
		return this;
	}

	ValuePromise<int> ISender.GetOnlinePlayerCount()
	{
		ValuePromise<int> promise = new ValuePromise<int>();
		ISignalWriter writer = CreatePromiseSignal(-632556091, promise /*hash 'GetOnlinePlayerCount'*/);
		SendSignal(writer);
		return promise;
	}

	void ISender.LookForMatch()
	{
		ISignalWriter writer = CreateSignal(-625843621 /*hash 'LookForMatch'*/);
		SendSignal(writer);
	}

	protected virtual void FoundMatch(LobbyMatch match)
	{
	}

	protected virtual void FoundMatches(LobbyMatch[] matches)
	{
	}

	protected void ReceiveSignal_FoundMatch(ISignalReader reader)
	{
		LobbyMatch match = new LobbyMatch();
		match.Deserialize(reader);
		OnFoundMatch(match);
		FoundMatch(match);
	}

	protected void ReceiveSignal_FoundMatches(ISignalReader reader)
	{
		int matches_length = reader.ReadInt32();
		LobbyMatch[] matches = new LobbyMatch[matches_length];
		for (int matches_x = 0; matches_x < matches_length; matches_x++)
		{
			matches[matches_x] = new LobbyMatch();
			matches[matches_x].Deserialize(reader);
		}
		OnFoundMatches(matches);
		FoundMatches(matches);
	}

	protected void ReceivePromise_GetOnlinePlayerCount(ISignalReader reader)
	{
		ReceivePromise(reader);
	}

}
