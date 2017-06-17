using NetworkScopes;

namespace MyExamples
{
	[Generated]
	public partial class PlayerData : ISerializable
	{
		public void Serialize(ISignalWriter writer)
		{
			writer.WriteInt32(playerID);
			writer.WriteString(playerName);
		}

		public void Deserialize(ISignalReader reader)
		{
			playerID = reader.ReadInt32();
			playerName = reader.ReadString();
		}

	}
}
