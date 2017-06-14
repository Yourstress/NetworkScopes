
namespace MyExamples
{
	using NetworkScopes;

	[Scope(typeof(IExampleClientScope))]
	public interface IExampleServerScope : IServerScope
	{
		void JoinGame(string gameName, int gameID);
	}
}