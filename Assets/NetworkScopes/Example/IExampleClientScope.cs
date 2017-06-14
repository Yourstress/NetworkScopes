
namespace MyExamples
{
	using NetworkScopes;

	[Scope(typeof(IExampleServerScope))]
	public interface IExampleClientScope : IClientScope
	{
		void OnPlayerJoined(string playerName, int playerID);
		void OnPlayerDataReceived(PlayerData playerData);
	}
}