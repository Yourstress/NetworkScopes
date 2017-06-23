using NetworkScopes;

[Generated]
public class MyClientLobby : ClientScope<MyClientLobby.ISender>, MyClientLobby.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		ValuePromise<int> GetOnlinePlayerCount();
		void JoinAnyMatch();
	}

	public delegate void FoundMatchDelegate();

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

	void ISender.JoinAnyMatch()
	{
		ISignalWriter writer = CreateSignal(-1643540829 /*hash 'JoinAnyMatch'*/);
		SendSignal(writer);
	}

	protected virtual void FoundMatch()
	{
	}

	protected void ReceiveSignal_FoundMatch(ISignalReader reader)
	{
		OnFoundMatch();
		FoundMatch();
	}

	protected void ReceivePromise_GetOnlinePlayerCount(ISignalReader reader)
	{
		ReceivePromise(reader);
	}

}
