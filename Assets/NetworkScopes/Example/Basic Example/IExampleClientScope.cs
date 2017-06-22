
namespace MyExamples
{
	using NetworkScopes;

	[ClientScope(typeof(IExampleServerScope))]
	public interface IExampleClientScope
	{
		void OnPlayerJoined(string playerName, int playerID);
		void OnPlayerDataReceived(PlayerData playerData);
	}
}