using NetworkScopes;

namespace MyExamples
{
	[Generated]
	public abstract class ExampleServerScope_Abstract : ServerScope<ExampleServerScope_Abstract.ISender>, ExampleServerScope_Abstract.ISender
	{

		[Generated]
		public interface ISender : IScopeSender
		{
			void OnPlayerJoined(string playerName, int playerID);
			void OnPlayerDataReceived(PlayerData playerData);
		}

		protected override ISender GetScopeSender()
		{
			return this;
		}

		void ISender.OnPlayerJoined(string playerName, int playerID)
		{
			ISignalWriter writer = CreateSignal(450541865 /*hash 'OnPlayerJoined'*/);
			writer.WriteString(playerName);
			writer.WriteInt32(playerID);
			SendSignal(writer);
		}

		void ISender.OnPlayerDataReceived(PlayerData playerData)
		{
			ISignalWriter writer = CreateSignal(-1141998197 /*hash 'OnPlayerDataReceived'*/);
			playerData.Serialize(writer);
			SendSignal(writer);
		}

		protected abstract void JoinGame(string playerName);
		protected void ReceiveSignal_JoinGame(ISignalReader reader)
		{
			string playerName = reader.ReadString();
			JoinGame(playerName);
		}

	}
}
