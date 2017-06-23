using NetworkScopes;

[Generated]
public class MyClientMatch : ClientScope<MyClientMatch.ISender>, MyClientMatch.ISender
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
