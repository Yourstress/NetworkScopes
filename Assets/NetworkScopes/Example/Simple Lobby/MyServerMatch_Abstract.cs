using NetworkScopes;

[Generated]
public abstract class MyServerMatch_Abstract : ServerScope<MyServerMatch_Abstract.ISender>, MyServerMatch_Abstract.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
	}

	protected override ISender GetScopeSender()
	{
		return this;
	}

	protected abstract void LeaveMatch();
	protected void ReceiveSignal_LeaveMatch(ISignalReader reader)
	{
		LeaveMatch();
	}

}
