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

	public event FoundMatchDelegate OnFoundMatch = delegate {};

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

	protected void ReceiveSignal_FoundMatch(ISignalReader reader)
	{
		LobbyMatch match = new LobbyMatch();
		match.Deserialize(reader);
		OnFoundMatch(match);
		FoundMatch(match);
	}

	protected void ReceivePromise_GetOnlinePlayerCount(ISignalReader reader)
	{
		ReceivePromise(reader);
	}

}
