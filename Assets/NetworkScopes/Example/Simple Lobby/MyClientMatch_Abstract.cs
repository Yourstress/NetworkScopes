using NetworkScopes;

[Generated]
public abstract class MyClientMatch_Abstract : ServerScope<MyClientMatch_Abstract.ISender>, MyClientMatch_Abstract.ISender
{

	[Generated]
	public interface ISender : IScopeSender
	{
	}

	protected override ISender GetScopeSender()
	{
		return this;
	}

}
