using NetworkScopes;

[Generated]
public class MyClientMatch : ClientScope<MyClientMatch.ISender>, MyClientMatch.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
		void LeaveMatch();
	}

	protected override ISender GetScopeSender()
	{
		return this;
	}

	void ISender.LeaveMatch()
	{
		ISignalWriter writer = CreateSignal(1946358126 /*hash 'LeaveMatch'*/);
		SendSignal(writer);
	}

}
