
namespace MyExamples
{
	using NetworkScopes;

	[ServerScope(typeof(IExampleClientScope))]
	public interface IExampleServerScope
	{
		void JoinGame(string playerName);
	}
}