using NetworkScopes;

namespace MyExamples
{
	[Generated]
	public class ExampleServerScope : ServerScope<ExampleServerScope.ISender>, ExampleServerScope.ISender
	{

		[Generated]
		public interface ISender : IScopeSender
		{
			void OnPlayerJoined(string playerName, int playerID);
			void OnPlayerDataReceived(PlayerData playerData);
		}

		public delegate void JoinGameDelegate(string gameName, int gameID);

		public event JoinGameDelegate OnJoinGame = delegate {};

		protected override ISender GetScopeSender()
		{
			return this;
		}

		void ISender.OnPlayerJoined(string playerName, int playerID)
		{
			ISignalWriter writer = CreateSignal(-2096698055);
			writer.WriteString(playerName);
			writer.WriteInt32(playerID);
			SendSignal(writer);
		}

		void ISender.OnPlayerDataReceived(PlayerData playerData)
		{
			ISignalWriter writer = CreateSignal(1703372187);
			playerData.Serialize(writer);
			SendSignal(writer);
		}

		protected virtual void JoinGame(string gameName, int gameID)
		{
		}

		protected void Receive_JoinGame(ISignalReader reader)
		{
			string gameName = reader.ReadString();
			int gameID = reader.ReadInt32();
			OnJoinGame(gameName, gameID);
			JoinGame(gameName, gameID);
		}

	}
}
