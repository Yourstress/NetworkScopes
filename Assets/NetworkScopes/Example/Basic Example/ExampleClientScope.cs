using NetworkScopes;

namespace MyExamples
{
	[Generated]
	public class ExampleClientScope : ClientScope<ExampleClientScope.ISender>, ExampleClientScope.ISender
	{

		[Generated]
		public interface ISender : IScopeSender
		{
			void JoinGame(string playerName);
		}

		public delegate void OnPlayerJoinedDelegate(string playerName, int playerID);
		public delegate void OnPlayerDataReceivedDelegate(PlayerData playerData);

		public event OnPlayerJoinedDelegate OnOnPlayerJoined = delegate {};
		public event OnPlayerDataReceivedDelegate OnOnPlayerDataReceived = delegate {};

		protected override ISender GetScopeSender()
		{
			return this;
		}

		void ISender.JoinGame(string playerName)
		{
			ISignalWriter writer = CreateSignal(-1337500580 /*hash 'JoinGame'*/);
			writer.WriteString(playerName);
			SendSignal(writer);
		}

		protected virtual void OnPlayerJoined(string playerName, int playerID)
		{
		}

		protected virtual void OnPlayerDataReceived(PlayerData playerData)
		{
		}

		protected void ReceiveSignal_OnPlayerJoined(ISignalReader reader)
		{
			string playerName = reader.ReadString();
			int playerID = reader.ReadInt32();
			OnOnPlayerJoined(playerName, playerID);
			OnPlayerJoined(playerName, playerID);
		}

		protected void ReceiveSignal_OnPlayerDataReceived(ISignalReader reader)
		{
			PlayerData playerData = new PlayerData();
			playerData.Deserialize(reader);
			OnOnPlayerDataReceived(playerData);
			OnPlayerDataReceived(playerData);
		}

	}
}
