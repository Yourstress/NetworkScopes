
namespace MyCompany
{
	using NetworkScopes;

	public interface IExampleServerMatch : IServerScope<ExampleMatchPeer>
	{
		void Signal(int x, string s, bool y);
	}
}